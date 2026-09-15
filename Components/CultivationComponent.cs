using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using SandboxTuTien.Core;
using SandboxTuTien.Core.Combat;
using SandboxTuTien.Data.Models;

namespace SandboxTuTien.Components
{
    // ========================================================================
    // ENUMS
    // ========================================================================

    /// <summary>
    /// Trạng thái FSM của nhân vật trong hệ thống tu luyện.
    /// </summary>
    public enum CultivationState
    {
        /// <summary>Nhàn rỗi — không tu luyện.</summary>
        Idle,

        /// <summary>Đả Tọa — dẫn linh khí nhập thể, tích lũy tu vi.</summary>
        Meditating,

        /// <summary>Chạm bình cảnh — tu vi bị khóa, cần đột phá.</summary>
        BreakthroughReady,

        /// <summary>Đang đột phá — xung quan (dưới Kim Đan) hoặc độ Thiên Kiếp (từ Kim Đan).</summary>
        Breakthrough,

        /// <summary>Tử vong — HP về 0 do Yêu Thú tấn công.</summary>
        Dead
    }

    /// <summary>
    /// Đại cảnh giới tu tiên. Mỗi cảnh giới gồm 10 tầng tu vi.
    /// Luyện Khí(1-10) → ... → Chân Tiên(91-99) → Tiên Đế(100).
    /// </summary>
    public enum CultivationRealm
    {
        LuyenKhi,   // Luyện Khí: Tầng 1-10
        TrucCo,     // Trúc Cơ: 11-20
        KimDan,     // Kim Đan: 21-30
        NguyenAnh,  // Nguyên Anh: 31-40
        HoaThan,    // Hóa Thần: 41-50
        LuyenHu,    // Luyện Hư: 51-60
        HopThe,     // Hợp Thể: 61-70
        DaiThua,    // Đại Thừa: 71-80
        DoKiep,     // Độ Kiếp: 81-90
        ChanTien,   // Chân Tiên: 91-99
        TienDe      // Tiên Đế: 100
    }

    // ========================================================================
    // CULTIVATION COMPONENT
    // ========================================================================

    /// <summary>
    /// Component chính quản lý toàn bộ logic tu luyện của một nhân vật.
    /// Bao gồm: tích lũy tu vi, tăng tầng, chạm bình cảnh, đột phá (xung quan / Thiên Kiếp),
    /// Linh Căn, Đan Dược trợ lực và Tâm Ma.
    ///
    /// Thiết kế theo Component-Based Architecture:
    /// - Không chứa logic render/input.
    /// - Chỉ chứa data + logic nghiệp vụ tu luyện.
    /// - Giao tiếp với bên ngoài qua EventManager (loosely coupled).
    /// </summary>
    public class CultivationComponent
    {
        // ====================================================================
        // CONSTANTS
        // ====================================================================

        /// <summary>Tầng tu vi tối đa.</summary>
        private const int MAX_LEVEL = 100;

        /// <summary>Tốc độ tu luyện cơ bản (tu vi/giây) khi Đả Tọa.</summary>
        private const float BASE_MEDITATION_RATE = 5.0f;

        /// <summary>Tu vi cơ bản cần để lên tầng (sẽ scale theo tầng).</summary>
        private const float BASE_EXP_PER_LEVEL = 100.0f;

        /// <summary>Từ bình cảnh tầng này trở lên phải độ Thiên Kiếp (Kim Đan viên mãn → Nguyên Anh).</summary>
        private const int HEAVENLY_TRIBULATION_LEVEL = 30;

        /// <summary>Số đợt phản phệ khi xung quan (bình cảnh dưới Kim Đan).</summary>
        private const int INNER_WAVES = 5;

        /// <summary>Khoảng cách giữa các đợt phản phệ (giây thực).</summary>
        private const float INNER_WAVE_INTERVAL = 0.6f;

        /// <summary>Khoảng cách giữa các đợt lôi kiếp (giây thực).</summary>
        private const float LIGHTNING_WAVE_INTERVAL = 1.2f;

        /// <summary>Thời gian từ lúc báo hiệu tới lúc sét đánh xuống (Game1 dùng chung hằng số này).</summary>
        public const float LIGHTNING_STRIKE_DELAY = 0.8f;

        /// <summary>Trợ lực tối đa từ Đan Dược.</summary>
        private const float MAX_PILL_BUFF = 0.5f;

        /// <summary>Tâm Ma tối đa.</summary>
        private const float MAX_HEART_DEMON = 0.5f;

        /// <summary>Tâm Ma tăng thêm sau mỗi lần đột phá thất bại.</summary>
        private const float HEART_DEMON_PER_FAILURE = 0.1f;

        // ====================================================================
        // PROPERTIES — Trạng thái tu luyện
        // ====================================================================

        /// <summary>Tên chủ sở hữu (để log).</summary>
        public string OwnerName { get; set; } = "Unknown";

        /// <summary>Tầng tu vi hiện tại (1-100).</summary>
        public int CurrentLevel { get; private set; }

        /// <summary>Tu vi tích lũy trong tầng hiện tại.</summary>
        public float CurrentExp { get; private set; }

        /// <summary>Tu vi cần để lên tầng tiếp theo.</summary>
        public float MaxExpForCurrentLevel => CalculateExpRequired(CurrentLevel);

        /// <summary>
        /// Phẩm chất Linh Căn (0.0 → 2.0), nhân trực tiếp với tốc độ tu luyện.
        /// 0.0 = Phế Linh Căn, 1.0 = Tạp/Chân Linh Căn, 2.0 = Thiên Linh Căn.
        /// </summary>
        public float SpiritRootMultiplier { get; set; }

        /// <summary>Hệ của Linh Căn — quyết định Pháp Thuật lĩnh ngộ khi đột phá.</summary>
        public Element SpiritRootElement { get; set; }

        /// <summary>Trạng thái FSM hiện tại.</summary>
        public CultivationState CurrentState { get; private set; }

        /// <summary>Đại cảnh giới hiện tại.</summary>
        public CultivationRealm CurrentRealm { get; private set; }

        /// <summary>Số lần đã đột phá bình cảnh thành công.</summary>
        public int BreakthroughCount { get; private set; }

        // ====================================================================
        // PROPERTIES — Chỉ số sinh tồn
        // ====================================================================

        /// <summary>HP hiện tại.</summary>
        public float HP { get; private set; }

        /// <summary>HP tối đa (scale theo tầng).</summary>
        public float MaxHP { get; private set; }

        /// <summary>Linh Lực hiện tại.</summary>
        public float SpiritPower { get; private set; }

        /// <summary>Linh Lực tối đa.</summary>
        public float MaxSpiritPower { get; private set; }

        /// <summary>Trợ lực đột phá từ Đan Dược đã dùng (0 → 0.5). Tiêu hết sau mỗi lần đột phá.</summary>
        public float PillBuff { get; private set; }

        /// <summary>Tâm Ma (0 → 0.5) — trừ vào tỷ lệ đột phá, tăng khi thất bại, tiêu tan khi thành công.</summary>
        public float HeartDemon { get; private set; }

        /// <summary>Pháp Thuật chủ động thứ nhất (phím Q), lĩnh ngộ sau lần đột phá đầu.</summary>
        public TechniqueData? Skill1 { get; private set; }

        /// <summary>Pháp Thuật chủ động thứ hai (phím E), lĩnh ngộ sau lần đột phá thứ hai.</summary>
        public TechniqueData? Skill2 { get; private set; }

        /// <summary>Cờ đánh dấu người chơi sở hữu thể chất đặc biệt Vạn Độc Thể.</summary>
        public bool HasVanDocThe { get; private set; }

        /// <summary>Số lần ấn Spacebar "Ổn định đạo tâm" trong quá trình đột phá.</summary>
        public int QTEPressCount { get; set; }

        /// <summary>Thời gian còn lại của hiệu ứng hồi HP theo thời gian.</summary>
        public float HoTTimer { get; private set; }

        /// <summary>Tỷ lệ HP tối đa hồi mỗi giây khi có HoT.</summary>
        private float _hotPercentPerSecond;

        /// <summary>True nếu lần đột phá hiện tại là Thiên Kiếp (lôi kiếp có thể né).</summary>
        public bool IsHeavenlyTribulation { get; private set; }

        /// <summary>Đợt phản phệ/lôi kiếp hiện tại (cho HUD).</summary>
        public int TribulationWave => _currentWave;

        /// <summary>Tổng số đợt của lần đột phá hiện tại (cho HUD).</summary>
        public int TribulationTotalWaves => _totalWaves;

        /// <summary>Có thể nhận thêm tu vi không (không chết, không đang bình cảnh/đột phá).</summary>
        public bool CanGainExp => CurrentState != CultivationState.Dead &&
                                  CurrentState != CultivationState.BreakthroughReady &&
                                  CurrentState != CultivationState.Breakthrough;

        /// <summary>Hồi HP (không vượt quá MaxHP) hoặc trừ HP nếu truyền số âm.</summary>
        public void Heal(float amount)
        {
            if (CurrentState == CultivationState.Dead) return;
            HP = Math.Clamp(HP + amount, 0f, MaxHP);
            if (amount > 0)
            {
                Console.WriteLine($"[Hồi HP] {OwnerName} được hồi {amount:F0} HP → HP: {HP:F0}/{MaxHP:F0}");
            }
            else if (amount < 0)
            {
                Console.WriteLine($"[Sát Thương] {OwnerName} chịu {-amount:F0} sát thương → HP: {HP:F0}/{MaxHP:F0}");
                if (HP <= 0)
                {
                    HP = 0;
                    CurrentState = CultivationState.Dead;
                    Console.WriteLine($"[TỬ VONG] ☠ {OwnerName} đã thân tử đạo tiêu!");
                    _eventManager.Publish(new OnPlayerDiedEvent
                    {
                        PlayerName = OwnerName,
                        CauseOfDeath = "Bị Yêu Thú tấn công chí mạng"
                    });
                }
            }
        }

        /// <summary>Kích hoạt hồi HP theo thời gian (Hồi Xuân Đan).</summary>
        public void ApplyHealOverTime(float percentPerSecond, float duration)
        {
            if (CurrentState == CultivationState.Dead) return;
            _hotPercentPerSecond = percentPerSecond;
            HoTTimer = duration;
        }

        /// <summary>Hồi Linh Lực (không vượt quá MaxSpiritPower).</summary>
        public void RecoverSpiritPower(float amount)
        {
            if (CurrentState == CultivationState.Dead) return;
            SpiritPower = Math.Clamp(SpiritPower + amount, 0f, MaxSpiritPower);
            Console.WriteLine($"[Hồi Linh Lực] {OwnerName} được hồi {amount:F0} Linh Lực → {SpiritPower:F0}/{MaxSpiritPower:F0}");
        }

        /// <summary>Tiêu hao Linh Lực.</summary>
        public bool ConsumeSpiritPower(float amount)
        {
            if (CurrentState == CultivationState.Dead || SpiritPower < amount) return false;
            SpiritPower -= amount;
            return true;
        }

        /// <summary>Cộng trợ lực đột phá từ Đan Dược (tối đa MAX_PILL_BUFF).</summary>
        public void AddPillBuff(float amount)
        {
            if (CurrentState == CultivationState.Dead) return;
            PillBuff = Math.Min(MAX_PILL_BUFF, PillBuff + amount);
            Console.WriteLine($"[Đan Dược] {OwnerName} luyện hóa dược lực → Trợ lực đột phá: +{PillBuff:P0}");
        }

        // ====================================================================
        // INTERNAL STATE — Đột phá
        // ====================================================================

        /// <summary>Thời gian đã trôi qua trong quá trình đột phá.</summary>
        private float _elapsed;

        /// <summary>Thời điểm (theo _elapsed) của đợt gần nhất.</summary>
        private float _lastWaveTime;

        /// <summary>Đợt hiện tại.</summary>
        private int _currentWave;

        /// <summary>Tổng số đợt.</summary>
        private int _totalWaves;

        /// <summary>Thời gian giữa mỗi đợt.</summary>
        private float _waveInterval;

        /// <summary>Bộ đếm thời gian cho đợt tiếp theo.</summary>
        private float _waveTimer;

        // ====================================================================
        // DEPENDENCIES
        // ====================================================================

        private readonly EventManager _eventManager;

        /// <summary>Danh sách Pháp Thuật (data-driven) để lĩnh ngộ theo Linh Căn.</summary>
        private readonly IReadOnlyList<TechniqueData> _techniques;

        /// <summary>Random generator cho các tính toán xác suất.</summary>
        private readonly Random _random = new();

        // ====================================================================
        // LOG THROTTLING
        // ====================================================================

        /// <summary>Bộ đếm để không spam log mỗi frame.</summary>
        private float _logTimer;
        private const float LOG_INTERVAL = 2.0f; // Log mỗi 2 giây thực

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        /// <summary>
        /// Khởi tạo CultivationComponent.
        /// </summary>
        /// <param name="eventManager">EventBus để publish sự kiện.</param>
        /// <param name="techniques">Danh sách Pháp Thuật nạp từ techniques.json.</param>
        /// <param name="innateLevel">Tầng tu vi ban đầu (0-10).</param>
        /// <param name="spiritRootMultiplier">Phẩm chất Linh Căn (0.0-2.0).</param>
        /// <param name="spiritRootElement">Hệ Linh Căn.</param>
        public CultivationComponent(EventManager eventManager, IReadOnlyList<TechniqueData> techniques,
                                    int innateLevel, float spiritRootMultiplier, Element spiritRootElement)
        {
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
            _techniques = techniques ?? throw new ArgumentNullException(nameof(techniques));

            CurrentLevel = Math.Clamp(innateLevel, 0, 10);
            SpiritRootMultiplier = Math.Clamp(spiritRootMultiplier, 0f, 2.0f);
            SpiritRootElement = spiritRootElement;
            BreakthroughCount = 0;
            // Khởi đầu đúng mốc bình cảnh (VD: tầng 10) thì phải đột phá trước
            CurrentState = IsAtBottleneck() ? CultivationState.BreakthroughReady : CultivationState.Idle;
            CurrentRealm = GetRealmForLevel(CurrentLevel);

            MaxHP = CalculateMaxHP(CurrentLevel);
            HP = MaxHP;
            MaxSpiritPower = CalculateMaxSpiritPower(CurrentLevel);
            SpiritPower = MaxSpiritPower;

            Console.WriteLine($"[Khởi Tạo] {OwnerName} — {GetSpiritRootName()}, " +
                              $"Cảnh giới: {GetRealmDisplayName(CurrentRealm)} tầng {CurrentLevel}, " +
                              $"HP: {HP}/{MaxHP}, Linh Lực: {SpiritPower}/{MaxSpiritPower}");
        }

        // ====================================================================
        // UPDATE — Gọi mỗi frame, nhân với deltaTime
        // ====================================================================

        /// <summary>
        /// Cập nhật logic tu luyện mỗi frame.
        /// Tất cả tính toán nhân với deltaTime để frame-rate independent.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Hồi phục HP theo thời gian (Hồi Xuân Đan)
            if (HoTTimer > 0)
            {
                HoTTimer -= deltaTime;
                HP = Math.Clamp(HP + MaxHP * _hotPercentPerSecond * deltaTime, 0f, MaxHP);
            }

            switch (CurrentState)
            {
                case CultivationState.Meditating:
                    UpdateMeditating(deltaTime);
                    break;

                case CultivationState.Breakthrough:
                    UpdateBreakthrough(deltaTime);
                    break;

                // Idle / BreakthroughReady / Dead: chờ lệnh
            }
        }

        // ====================================================================
        // STATE TRANSITIONS — Chuyển trạng thái
        // ====================================================================

        /// <summary>Bắt đầu Đả Tọa (tu luyện).</summary>
        public void StartMeditating()
        {
            if (CurrentState == CultivationState.Dead)
            {
                Console.WriteLine($"[Tu Luyện] {OwnerName} đã tử vong, không thể tu luyện!");
                return;
            }

            if (CurrentState == CultivationState.BreakthroughReady)
            {
                Console.WriteLine($"[Tu Luyện] {OwnerName} đã chạm bình cảnh! Phải đột phá mới tu luyện tiếp được.");
                return;
            }

            if (CurrentState == CultivationState.Breakthrough)
            {
                Console.WriteLine($"[Tu Luyện] {OwnerName} đang đột phá, không thể phân tâm!");
                return;
            }

            if (SpiritRootMultiplier <= 0f)
            {
                Console.WriteLine($"[Tu Luyện] {OwnerName} là Phế Linh Căn — không thể dẫn khí nhập thể!");
                return;
            }

            CurrentState = CultivationState.Meditating;
            Console.WriteLine($"[Tu Luyện] ★ {OwnerName} bắt đầu Đả Tọa... " +
                              $"Tầng {CurrentLevel} ({CurrentExp:F0}/{MaxExpForCurrentLevel:F0} tu vi)");
        }

        /// <summary>Dừng Đả Tọa, về trạng thái Idle.</summary>
        public void StopMeditating()
        {
            if (CurrentState == CultivationState.Meditating)
            {
                CurrentState = CultivationState.Idle;
                Console.WriteLine($"[Tu Luyện] {OwnerName} thu công. Tu vi: {CurrentExp:F0}/{MaxExpForCurrentLevel:F0}");
            }
        }

        /// <summary>
        /// Bắt đầu đột phá bình cảnh. Chỉ gọi được khi đang ở BreakthroughReady.
        /// Dưới Kim Đan: xung quan (phản phệ nội tại). Từ Kim Đan: độ Thiên Kiếp (lôi kiếp có thể né).
        /// </summary>
        public bool StartBreakthrough()
        {
            if (CurrentState != CultivationState.BreakthroughReady)
            {
                Console.WriteLine($"[Đột Phá] {OwnerName} chưa chạm bình cảnh! Trạng thái hiện tại: {CurrentState}");
                return false;
            }

            IsHeavenlyTribulation = CurrentLevel >= HEAVENLY_TRIBULATION_LEVEL;
            _totalWaves = IsHeavenlyTribulation ? 2 + CurrentLevel / 10 : INNER_WAVES;
            _waveInterval = IsHeavenlyTribulation ? LIGHTNING_WAVE_INTERVAL : INNER_WAVE_INTERVAL;
            _currentWave = 0;
            _waveTimer = 0f;
            _elapsed = 0f;
            _lastWaveTime = 0f;
            QTEPressCount = 0;

            CurrentState = CultivationState.Breakthrough;

            Console.WriteLine(IsHeavenlyTribulation
                ? $"[Thiên Kiếp] ⚡ {OwnerName} dẫn động Thiên Kiếp! {_totalWaves} đợt lôi kiếp sắp giáng xuống!"
                : $"[Xung Quan] ⚡ {OwnerName} bắt đầu xung kích bình cảnh tầng {CurrentLevel}!");
            Console.WriteLine($"           Tỷ lệ thành công: {CalculateBreakthroughSuccessRate():P1} " +
                              $"(Đan dược +{PillBuff:P0}, Tâm Ma -{HeartDemon:P0})");
            return true;
        }

        /// <summary>Nhận sát thương từ một tia lôi kiếp đánh trúng (do Game1 gọi).</summary>
        public void TakeTribulationDamage(float damage)
        {
            if (CurrentState != CultivationState.Breakthrough) return;
            ApplyBreakthroughDamage(damage, "Lôi kiếp giáng trúng thân");
        }

        /// <summary>Thêm tu vi trực tiếp (Yêu Đan hoặc debug). Trả về false nếu đang bị khóa.</summary>
        public bool AddExp(float amount)
        {
            if (!CanGainExp)
            {
                Console.WriteLine($"[Tu Luyện] {OwnerName} không thể hấp thu thêm tu vi lúc này ({CurrentState}).");
                return false;
            }

            CurrentExp += amount;
            Console.WriteLine($"[Tu Vi] +{amount:F0} → {OwnerName}: {CurrentExp:F0}/{MaxExpForCurrentLevel:F0}");

            CheckLevelUp();
            return true;
        }

        // ====================================================================
        // PRIVATE — Logic Đả Tọa
        // ====================================================================

        private void UpdateMeditating(float deltaTime)
        {
            // Tốc độ tăng tu vi = Base × Linh Căn × deltaTime
            float expGain = BASE_MEDITATION_RATE * SpiritRootMultiplier * deltaTime;
            CurrentExp += expGain;

            // Throttled logging
            _logTimer += deltaTime;
            if (_logTimer >= LOG_INTERVAL)
            {
                _logTimer = 0f;
                Console.WriteLine($"[Đả Tọa] {OwnerName} đang tu luyện... " +
                                  $"Tầng {CurrentLevel} — Tu vi: {CurrentExp:F1}/{MaxExpForCurrentLevel:F0} " +
                                  $"(+{expGain / deltaTime:F1}/s)");
            }

            CheckLevelUp();
        }

        private void CheckLevelUp()
        {
            while (CurrentExp >= MaxExpForCurrentLevel && CurrentLevel < MAX_LEVEL)
            {
                CurrentExp -= MaxExpForCurrentLevel;
                int oldLevel = CurrentLevel;
                CurrentLevel++;

                MaxHP = CalculateMaxHP(CurrentLevel);
                HP = MaxHP;
                MaxSpiritPower = CalculateMaxSpiritPower(CurrentLevel);
                SpiritPower = MaxSpiritPower;

                Console.WriteLine($"[TĂNG TẦNG] ★★★ {OwnerName} tu vi tăng lên tầng {CurrentLevel}! ★★★");

                _eventManager.Publish(new OnLevelUpEvent
                {
                    PlayerName = OwnerName,
                    OldLevel = oldLevel,
                    NewLevel = CurrentLevel
                });

                var newRealm = GetRealmForLevel(CurrentLevel);
                if (newRealm != CurrentRealm)
                {
                    var oldRealm = CurrentRealm;
                    CurrentRealm = newRealm;

                    Console.WriteLine($"[CẢNH GIỚI] ✦✦✦ {OwnerName} bước vào {GetRealmDisplayName(newRealm)}! ✦✦✦");

                    _eventManager.Publish(new OnRealmChangedEvent
                    {
                        PlayerName = OwnerName,
                        OldRealm = GetRealmDisplayName(oldRealm),
                        NewRealm = GetRealmDisplayName(newRealm),
                        Level = CurrentLevel
                    });
                }

                if (IsAtBottleneck())
                {
                    CurrentState = CultivationState.BreakthroughReady;
                    CurrentExp = 0; // Khóa tu vi
                    bool heavenly = CurrentLevel >= HEAVENLY_TRIBULATION_LEVEL;

                    Console.WriteLine($"[BÌNH CẢNH] ⚠ {OwnerName} chạm bình cảnh tại tầng {CurrentLevel}!");
                    Console.WriteLine(heavenly
                        ? "            Phải độ Thiên Kiếp để đột phá. Chuẩn bị Phá Cảnh Đan!"
                        : "            Phải xung kích bình cảnh để đột phá.");

                    _eventManager.Publish(new OnBottleneckReachedEvent
                    {
                        PlayerName = OwnerName,
                        Level = CurrentLevel,
                        CurrentRealm = GetRealmDisplayName(CurrentRealm),
                        IsHeavenlyTribulation = heavenly
                    });

                    break; // Dừng tăng tầng tại bình cảnh
                }
            }
        }

        // ====================================================================
        // PRIVATE — Logic Đột Phá
        // ====================================================================

        private void UpdateBreakthrough(float deltaTime)
        {
            _elapsed += deltaTime;
            _waveTimer += deltaTime;

            if (_waveTimer >= _waveInterval && _currentWave < _totalWaves)
            {
                _waveTimer = 0f;
                _currentWave++;
                _lastWaveTime = _elapsed;

                float successRate = CalculateBreakthroughSuccessRate();

                if (IsHeavenlyTribulation)
                {
                    // Lôi kiếp: sát thương cố định theo tỷ lệ, người chơi có thể né
                    int strikeCount = 1 + (CurrentLevel / 10 - 2) / 2;
                    float damage = MaxHP * (0.12f + 0.4f * (1f - successRate));

                    Console.WriteLine($"[Thiên Kiếp] Đợt {_currentWave}/{_totalWaves}: {strikeCount} đạo thiên lôi " +
                                      $"giáng xuống ({damage:F0} sát thương mỗi đạo)!");

                    _eventManager.Publish(new OnLightningStrikeEvent
                    {
                        PlayerName = OwnerName,
                        Wave = _currentWave,
                        TotalWaves = _totalWaves,
                        StrikeCount = strikeCount,
                        Damage = damage
                    });
                }
                else
                {
                    // Xung quan: linh lực phản phệ kinh mạch, không thể né
                    float variance = 0.3f + (float)_random.NextDouble() * 0.2f; // 0.3 - 0.5
                    float damage = MaxHP * (1f - successRate) * variance;
                    ApplyBreakthroughDamage(damage, $"Đợt {_currentWave}/{_totalWaves}: linh lực phản phệ kinh mạch");
                    if (CurrentState != CultivationState.Breakthrough) return;
                }
            }

            // Hoàn tất khi hết các đợt (Thiên Kiếp chờ tia sét cuối cùng đánh xuống)
            float settleTime = IsHeavenlyTribulation ? LIGHTNING_STRIKE_DELAY + 0.15f : 0.1f;
            if (_currentWave >= _totalWaves && _elapsed >= _lastWaveTime + settleTime)
            {
                CompleteBreakthrough();
            }
        }

        private void ApplyBreakthroughDamage(float damage, string description)
        {
            HP -= damage;
            Console.WriteLine($"[Đột Phá] {description}! -{damage:F0} HP → HP: {Math.Max(0f, HP):F0}/{MaxHP:F0}");

            if (HP <= 0)
            {
                FailBreakthrough();
            }
        }

        private void FailBreakthrough()
        {
            float successRate = CalculateBreakthroughSuccessRate();
            int oldLevel = CurrentLevel;

            // Đạo cơ tổn hại: rớt một tầng tu vi, Tâm Ma tăng, dược lực tiêu tán
            CurrentLevel = Math.Max(1, CurrentLevel - 1);
            CurrentExp = 0;
            CurrentRealm = GetRealmForLevel(CurrentLevel);
            MaxHP = CalculateMaxHP(CurrentLevel);
            HP = MaxHP * 0.2f;
            MaxSpiritPower = CalculateMaxSpiritPower(CurrentLevel);
            SpiritPower = Math.Min(SpiritPower, MaxSpiritPower);
            HeartDemon = Math.Min(MAX_HEART_DEMON, HeartDemon + HEART_DEMON_PER_FAILURE);
            PillBuff = 0f;
            CurrentState = CultivationState.Idle;

            string reason = IsHeavenlyTribulation
                ? "Không chống đỡ nổi Thiên Kiếp — đạo cơ tổn hại"
                : "Xung quan thất bại — kinh mạch nghịch loạn";

            Console.WriteLine($"[ĐỘT PHÁ THẤT BẠI] ✗✗✗ {OwnerName}: {reason}! ✗✗✗");
            Console.WriteLine($"          Tu vi rớt từ tầng {oldLevel} xuống tầng {CurrentLevel}. Tâm Ma: {HeartDemon:P0}");

            _eventManager.Publish(new OnBreakthroughFailedEvent
            {
                PlayerName = OwnerName,
                Reason = reason,
                SuccessRate = successRate,
                LevelLost = oldLevel - CurrentLevel
            });
        }

        private void CompleteBreakthrough()
        {
            BreakthroughCount++;
            bool wasHeavenly = IsHeavenlyTribulation;

            PillBuff = 0f;
            HeartDemon = 0f;
            CurrentState = CultivationState.Meditating;
            CurrentExp = 0;

            // 1% cơ duyên thức tỉnh Vạn Độc Thể sau khi đột phá
            if (!HasVanDocThe && _random.NextDouble() < 0.01)
            {
                UnlockVanDocThe();
            }

            MaxHP = CalculateMaxHP(CurrentLevel);
            HP = MaxHP;
            MaxSpiritPower = CalculateMaxSpiritPower(CurrentLevel);
            SpiritPower = MaxSpiritPower;

            // Lĩnh ngộ Pháp Thuật theo Linh Căn
            TechniqueData? learned = null;
            if (BreakthroughCount == 1)
            {
                Skill1 = FindTechnique(1);
                learned = Skill1;
            }
            else if (BreakthroughCount == 2)
            {
                Skill2 = FindTechnique(2);
                learned = Skill2;
            }

            Console.WriteLine($"[ĐỘT PHÁ] ★★★ {OwnerName} " +
                              (wasHeavenly ? "vượt qua Thiên Kiếp" : "phá vỡ bình cảnh") +
                              $" tầng {CurrentLevel} THÀNH CÔNG! ★★★");
            Console.WriteLine($"           Tiếp tục tu luyện để bước vào {GetRealmDisplayName(GetRealmForLevel(CurrentLevel + 1))}.");
            if (learned != null)
            {
                Console.WriteLine($"           Lĩnh ngộ Pháp Thuật: {learned.Name}");
            }

            _eventManager.Publish(new OnBreakthroughSuccessEvent
            {
                PlayerName = OwnerName,
                Level = CurrentLevel,
                BreakthroughNumber = BreakthroughCount,
                WasHeavenlyTribulation = wasHeavenly
            });

            if (learned != null)
            {
                _eventManager.Publish(new OnTechniqueLearnedEvent
                {
                    PlayerName = OwnerName,
                    TechniqueName = learned.Name,
                    Tier = learned.Tier
                });
            }
        }

        /// <summary>
        /// Thức tỉnh thể chất Vạn Độc Thể (tăng vĩnh viễn HP, Linh Lực và đòn đánh có thể tẩm độc).
        /// </summary>
        public void UnlockVanDocThe()
        {
            if (HasVanDocThe) return;
            HasVanDocThe = true;
            MaxHP = CalculateMaxHP(CurrentLevel);
            HP = Math.Clamp(HP + 50f, 0f, MaxHP);
            MaxSpiritPower = CalculateMaxSpiritPower(CurrentLevel);
            SpiritPower = Math.Clamp(SpiritPower + 30f, 0f, MaxSpiritPower);
            Console.WriteLine($"[CƠ DUYÊN] ★★★ {OwnerName} thức tỉnh VẠN ĐỘC THỂ! (+50 MaxHP, +30 Linh Lực, đòn đánh có 25% cơ hội tẩm độc) ★★★");
        }

        /// <summary>Tìm Pháp Thuật theo tier và hệ Linh Căn (không có thì dùng pháp thuật vô thuộc tính).</summary>
        private TechniqueData? FindTechnique(int tier)
        {
            return _techniques.FirstOrDefault(t => t.Tier == tier && ElementExtensions.ParseElement(t.Element) == SpiritRootElement)
                ?? _techniques.FirstOrDefault(t => t.Tier == tier && ElementExtensions.ParseElement(t.Element) == Element.None);
        }

        // ====================================================================
        // CALCULATIONS — Công thức tính toán
        // ====================================================================

        /// <summary>
        /// Tu vi cần để lên tầng = BASE × (1 + tầng × 0.5).
        /// </summary>
        private float CalculateExpRequired(int level)
        {
            return BASE_EXP_PER_LEVEL * (1f + level * 0.5f);
        }

        /// <summary>
        /// HP tối đa = 100 + tầng × 20 + 50 (nếu có Vạn Độc Thể).
        /// </summary>
        private float CalculateMaxHP(int level)
        {
            float baseHP = 100f + level * 20f;
            return HasVanDocThe ? baseHP + 50f : baseHP;
        }

        /// <summary>
        /// Linh Lực tối đa = 100 + tầng × 10 + 30 (nếu có Vạn Độc Thể).
        /// </summary>
        private float CalculateMaxSpiritPower(int level)
        {
            float baseSP = 100f + level * 10f;
            return HasVanDocThe ? baseSP + 30f : baseSP;
        }

        /// <summary>
        /// Tỷ lệ gốc của bình cảnh: tầng 10 = 85%, mỗi đại cảnh giới sau giảm 15%, tối thiểu 20%.
        /// </summary>
        private static float CalculateBaseBreakthroughRate(int level)
        {
            int bottleneckIndex = Math.Max(0, level / 10 - 1);
            return Math.Max(0.2f, 0.85f - 0.15f * bottleneckIndex);
        }

        /// <summary>
        /// Tỷ_Lệ_Thành_Công = Tỷ_Lệ_Gốc + Đan_Dược_Buff + Ổn_Định_Đạo_Tâm(2%/lần Space) − Tâm_Ma.
        /// Clamp trong [5%, 100%].
        /// </summary>
        public float CalculateBreakthroughSuccessRate()
        {
            float qteBuff = QTEPressCount * 0.02f;
            float rate = CalculateBaseBreakthroughRate(CurrentLevel) + PillBuff + qteBuff - HeartDemon;
            return Math.Clamp(rate, 0.05f, 1f);
        }

        /// <summary>
        /// Đang ở mốc bình cảnh (tầng 10, 20, ...) mà chưa đột phá mốc đó.
        /// </summary>
        private bool IsAtBottleneck()
        {
            return CurrentLevel > 0 && CurrentLevel < MAX_LEVEL &&
                   CurrentLevel % 10 == 0 && BreakthroughCount < CurrentLevel / 10;
        }

        // ====================================================================
        // STATIC HELPERS
        // ====================================================================

        /// <summary>Xác định đại cảnh giới dựa trên tầng tu vi.</summary>
        public static CultivationRealm GetRealmForLevel(int level)
        {
            return level switch
            {
                >= 100 => CultivationRealm.TienDe,
                >= 91 => CultivationRealm.ChanTien,
                >= 81 => CultivationRealm.DoKiep,
                >= 71 => CultivationRealm.DaiThua,
                >= 61 => CultivationRealm.HopThe,
                >= 51 => CultivationRealm.LuyenHu,
                >= 41 => CultivationRealm.HoaThan,
                >= 31 => CultivationRealm.NguyenAnh,
                >= 21 => CultivationRealm.KimDan,
                >= 11 => CultivationRealm.TrucCo,
                _ => CultivationRealm.LuyenKhi
            };
        }

        /// <summary>Trả về tên hiển thị tiếng Việt của cảnh giới.</summary>
        public static string GetRealmDisplayName(CultivationRealm realm)
        {
            return realm switch
            {
                CultivationRealm.LuyenKhi => "Luyện Khí",
                CultivationRealm.TrucCo => "Trúc Cơ",
                CultivationRealm.KimDan => "Kim Đan",
                CultivationRealm.NguyenAnh => "Nguyên Anh",
                CultivationRealm.HoaThan => "Hóa Thần",
                CultivationRealm.LuyenHu => "Luyện Hư",
                CultivationRealm.HopThe => "Hợp Thể",
                CultivationRealm.DaiThua => "Đại Thừa",
                CultivationRealm.DoKiep => "Độ Kiếp",
                CultivationRealm.ChanTien => "Chân Tiên",
                CultivationRealm.TienDe => "Tiên Đế",
                _ => "Không Rõ"
            };
        }

        /// <summary>Tên Linh Căn theo phẩm chất và hệ, VD: "Thiên Linh Căn (Hỏa)".</summary>
        public string GetSpiritRootName()
        {
            string quality = SpiritRootMultiplier switch
            {
                >= 1.8f => "Thiên Linh Căn",
                >= 1.2f => "Chân Linh Căn",
                >= 0.5f => "Tạp Linh Căn",
                > 0f => "Ngụy Linh Căn",
                _ => "Phế Linh Căn"
            };
            return SpiritRootMultiplier > 0f ? $"{quality} ({SpiritRootElement.GetDisplayName()})" : quality;
        }

        // ====================================================================
        // SERIALIZATION
        // ====================================================================

        /// <summary>
        /// Trả về chuỗi JSON biểu diễn trạng thái hiện tại.
        /// </summary>
        public string Serialize()
        {
            var data = new
            {
                ownerName = OwnerName,
                currentLevel = CurrentLevel,
                currentExp = CurrentExp,
                spiritRootMultiplier = SpiritRootMultiplier,
                spiritRootElement = SpiritRootElement.ToString(),
                currentState = CurrentState.ToString(),
                currentRealm = CurrentRealm.ToString(),
                breakthroughCount = BreakthroughCount,
                hp = HP,
                maxHp = MaxHP,
                spiritPower = SpiritPower,
                maxSpiritPower = MaxSpiritPower,
                pillBuff = PillBuff,
                heartDemon = HeartDemon
            };

            return System.Text.Json.JsonSerializer.Serialize(data);
        }

        /// <summary>
        /// Khôi phục trạng thái tu luyện từ hệ thống lưu trữ MySQL (Save/Load).
        /// </summary>
        public void LoadState(int level, float exp, float hp, float maxHp, float sp, float maxSp,
                              int breakthroughCount, bool hasVanDocThe, float heartDemon,
                              string realmStr, string skill1Id, string skill2Id)
        {
            CurrentLevel = level;
            CurrentExp = exp;
            HP = hp;
            MaxHP = maxHp;
            SpiritPower = sp;
            MaxSpiritPower = maxSp;
            BreakthroughCount = breakthroughCount;
            HasVanDocThe = hasVanDocThe;
            HeartDemon = Math.Clamp(heartDemon, 0f, MAX_HEART_DEMON);
            PillBuff = 0f;
            HoTTimer = 0f;

            CurrentRealm = Enum.TryParse<CultivationRealm>(realmStr, out var realm)
                ? realm
                : GetRealmForLevel(CurrentLevel);

            Skill1 = _techniques.FirstOrDefault(t => t.Id == skill1Id);
            Skill2 = _techniques.FirstOrDefault(t => t.Id == skill2Id);

            // Khôi phục trạng thái FSM (bình cảnh nếu chưa đột phá mốc hiện tại)
            if (HP > 0)
            {
                CurrentState = IsAtBottleneck() ? CultivationState.BreakthroughReady : CultivationState.Idle;
            }
            else
            {
                CurrentState = CultivationState.Dead;
            }
        }
    }
}
