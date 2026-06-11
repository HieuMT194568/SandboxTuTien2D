using System;
using Microsoft.Xna.Framework;
using SandboxTuTien.Core;

namespace SandboxTuTien.Components
{
    /// <summary>
    /// Định nghĩa chi tiết của một Hồn Kỹ học được từ Hồn Hoàn.
    /// </summary>
    public class SoulSkill
    {
        public string Name { get; set; } = string.Empty;
        public SandboxTuTien.Core.Combat.Element Element { get; set; }
        public int RingNumber { get; set; } // Hồn hoàn thứ mấy (1 hoặc 2)
        public float SPCost { get; set; }  // Hồn lực tiêu hao (mana)
        public float Damage { get; set; }  // Sát thương cơ bản

        public SoulSkill(string name, SandboxTuTien.Core.Combat.Element element, int ringNumber, float spCost, float damage)
        {
            Name = name;
            Element = element;
            RingNumber = ringNumber;
            SPCost = spCost;
            Damage = damage;
        }
    }
    // ========================================================================
    // ENUMS
    // ========================================================================

    /// <summary>
    /// Trạng thái FSM của nhân vật trong hệ thống tu luyện.
    /// Theo CONTEXT: [IDLE, MEDITATING, HUNTING_RING, ABSORBING_RING, DEAD]
    /// Bổ sung BREAKTHROUGH_READY làm trạng thái chờ khi chạm bình cảnh.
    /// </summary>
    public enum CultivationState
    {
        /// <summary>Nhàn rỗi — không tu luyện.</summary>
        Idle,

        /// <summary>Minh Tưởng — đang tu luyện tích lũy Hồn Lực.</summary>
        Meditating,

        /// <summary>Đã chạm bình cảnh — EXP bị khóa, cần săn Hồn Thú.</summary>
        BreakthroughReady,

        /// <summary>Đang săn Hồn Thú để lấy Hồn Hoàn.</summary>
        HuntingRing,

        /// <summary>Đang hấp thu Hồn Hoàn — rủi ro bạo thể.</summary>
        AbsorbingRing,

        /// <summary>Tử vong — cơ thể bạo liệt hoặc HP về 0.</summary>
        Dead
    }

    /// <summary>
    /// Cảnh giới (danh hiệu) theo hệ thống Đấu La Đại Lục.
    /// Mỗi cảnh giới tương ứng 10 cấp Hồn Lực.
    /// Hồn Sĩ(1-10) → ... → Thần(100).
    /// </summary>
    public enum CultivationRealm
    {
        HonSi,          // Hồn Sĩ: Cấp 1-10
        HonSu,          // Hồn Sư: Cấp 11-20
        DaiHonSu,       // Đại Hồn Sư: Cấp 21-30
        HonTon,         // Hồn Tôn: Cấp 31-40
        HonTong,        // Hồn Tông: Cấp 41-50
        HonVuong,       // Hồn Vương: Cấp 51-60
        HonDe,          // Hồn Đế: Cấp 61-70
        HonThanh,       // Hồn Thánh: Cấp 71-80
        HonDauLa,       // Hồn Đấu La: Cấp 81-90
        PhongHaoDauLa,  // Phong Hào Đấu La: Cấp 91-99
        Than            // Thần: Cấp 100
    }

    // ========================================================================
    // CULTIVATION COMPONENT
    // ========================================================================

    /// <summary>
    /// Component chính quản lý toàn bộ logic tu luyện của một nhân vật.
    /// Bao gồm: tích lũy EXP, tăng cấp, chạm bình cảnh, hấp thu Hồn Hoàn,
    /// và xử lý rủi ro bạo thể.
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

        /// <summary>Cấp Hồn Lực tối đa.</summary>
        private const int MAX_LEVEL = 100;

        /// <summary>Tốc độ tu luyện cơ bản (EXP/giây game) khi Minh Tưởng.</summary>
        private const float BASE_MEDITATION_RATE = 5.0f;

        /// <summary>EXP cơ bản cần để lên cấp (sẽ scale theo level).</summary>
        private const float BASE_EXP_PER_LEVEL = 100.0f;

        /// <summary>Số đợt internal damage khi hấp thu Hồn Hoàn.</summary>
        private const int ABSORPTION_DAMAGE_WAVES = 5;

        /// <summary>Thời gian hấp thu Hồn Hoàn (giây thực).</summary>
        private const float ABSORPTION_DURATION = 3.0f;

        // ====================================================================
        // PROPERTIES — Trạng thái tu luyện
        // ====================================================================

        /// <summary>Tên chủ sở hữu (để log).</summary>
        public string OwnerName { get; set; } = "Unknown";

        /// <summary>Cấp Hồn Lực hiện tại (1-100).</summary>
        public int CurrentLevel { get; private set; }

        /// <summary>EXP tích lũy trong cấp hiện tại.</summary>
        public float CurrentExp { get; private set; }

        /// <summary>EXP cần để lên cấp tiếp theo.</summary>
        public float MaxExpForCurrentLevel => CalculateExpRequired(CurrentLevel);

        /// <summary>
        /// Hệ số Tiên Thiên Hồn Lực (0.0 → 2.0).
        /// 0.0 = Phế Vũ Hồn, 1.0 = Thường, 2.0 = Tiên Thiên Mãn Hồn Lực.
        /// Nhân trực tiếp với tốc độ tu luyện.
        /// </summary>
        public float InnateMultiplier { get; set; }

        /// <summary>Trạng thái FSM hiện tại.</summary>
        public CultivationState CurrentState { get; private set; }

        /// <summary>Cảnh giới (danh hiệu) hiện tại.</summary>
        public CultivationRealm CurrentRealm { get; private set; }

        /// <summary>Số Hồn Hoàn đã hấp thu (tối đa 9 cho thường, ít hơn cho Song Sinh Vũ Hồn).</summary>
        public int SoulRingsCount { get; private set; }

        // ====================================================================
        // PROPERTIES — Chỉ số sinh tồn
        // ====================================================================

        /// <summary>HP hiện tại.</summary>
        public float HP { get; private set; }

        /// <summary>HP tối đa (scale theo level).</summary>
        public float MaxHP { get; private set; }

        /// <summary>Hồn Lực năng lượng hiện tại.</summary>
        public float SoulPower { get; private set; }

        /// <summary>Hồn Lực năng lượng tối đa.</summary>
        public float MaxSoulPower { get; private set; }

        /// <summary>
        /// Cực hạn chịu đựng cơ thể — số năm tu vi Hồn Thú tối đa
        /// mà cơ thể có thể hấp thu an toàn. Scale theo cảnh giới.
        /// </summary>
        public float BodyLimit { get; private set; }

        /// <summary>Buff ý chí (Willpower) — tăng tỷ lệ hấp thu thành công.</summary>
        public float WillpowerBuff { get; set; }

        /// <summary>Hồn Kỹ chủ động thứ nhất (phím Q) học được từ Hồn Hoàn 1.</summary>
        public SoulSkill? Skill1 { get; private set; }

        /// <summary>Hồn Kỹ chủ động thứ hai (phím W) học được từ Hồn Hoàn 2.</summary>
        public SoulSkill? Skill2 { get; private set; }

        /// <summary>Cờ đánh dấu người chơi sở hữu Ngoại Phụ Hồn Cốt Bát Chu Mâu.</summary>
        public bool HasBatChuMau { get; private set; }

        /// <summary>Số lần ấn Spacebar QTE trong quá trình hấp thu.</summary>
        public int QTEPressCount { get; set; }

        /// <summary>Bộ đếm thời gian hồi phục HP theo thời gian (Heal over Time).</summary>
        public float HoTTimer { get; set; }

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
                    Console.WriteLine($"[TỬ VONG] ☠ {OwnerName} đã kiệt sức tử vong!");
                    _eventManager.Publish(new OnPlayerDiedEvent
                    {
                        PlayerName = OwnerName,
                        CauseOfDeath = "Bị Hồn Thú tấn công chí mạng"
                    });
                }
            }
        }

        /// <summary>Hồi Hồn Lực năng lượng (không vượt quá MaxSoulPower).</summary>
        public void RecoverSoulPower(float amount)
        {
            if (CurrentState == CultivationState.Dead) return;
            SoulPower = Math.Clamp(SoulPower + amount, 0f, MaxSoulPower);
            Console.WriteLine($"[Hồi Hồn Lực] {OwnerName} được hồi {amount:F0} Hồn Lực → Hồn Lực: {SoulPower:F0}/{MaxSoulPower:F0}");
        }

        /// <summary>Tiêu hao Hồn Lực năng lượng.</summary>
        public bool ConsumeSoulPower(float amount)
        {
            if (CurrentState == CultivationState.Dead || SoulPower < amount) return false;
            SoulPower -= amount;
            return true;
        }

        // ====================================================================
        // INTERNAL STATE — Hấp thu Hồn Hoàn
        // ====================================================================

        /// <summary>Tuổi Hồn Thú đang hấp thu (năm tu vi).</summary>
        private int _absorbingSoulBeastAge;

        /// <summary>Tuổi Hồn Thú đang hấp thu (năm tu vi) công khai cho HUD.</summary>
        public int AbsorbingSoulBeastAge => _absorbingSoulBeastAge;

        /// <summary>Hệ thuộc tính của Hồn Thú đang hấp thu.</summary>
        private SandboxTuTien.Core.Combat.Element _absorbingSoulBeastElement;

        /// <summary>Thời gian đã trôi qua trong quá trình hấp thu.</summary>
        private float _absorptionElapsed;

        /// <summary>Đợt damage hiện tại trong quá trình hấp thu.</summary>
        private int _currentDamageWave;

        /// <summary>Thời gian giữa mỗi đợt damage.</summary>
        private float _timeBetweenWaves;

        /// <summary>Bộ đếm thời gian cho wave tiếp theo.</summary>
        private float _waveTimer;

        // ====================================================================
        // DEPENDENCIES
        // ====================================================================

        private readonly EventManager _eventManager;

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
        /// <param name="innateLevel">Cấp Tiên Thiên Hồn Lực ban đầu (0-10).</param>
        /// <param name="innateMultiplier">Hệ số tu luyện (0.0-2.0).</param>
        public CultivationComponent(EventManager eventManager, int innateLevel, float innateMultiplier)
        {
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));

            CurrentLevel = Math.Clamp(innateLevel, 0, 10);
            InnateMultiplier = Math.Clamp(innateMultiplier, 0f, 2.0f);
            CurrentState = CultivationState.Idle;
            CurrentRealm = GetRealmForLevel(CurrentLevel);
            SoulRingsCount = 0;
            WillpowerBuff = 0f;

            // Khởi tạo HP theo level
            MaxHP = CalculateMaxHP(CurrentLevel);
            HP = MaxHP;
            MaxSoulPower = CalculateMaxSoulPower(CurrentLevel);
            SoulPower = MaxSoulPower;
            BodyLimit = CalculateBodyLimit(SoulRingsCount);

            Console.WriteLine($"[Khởi Tạo] {OwnerName} — Tiên Thiên Hồn Lực: Cấp {CurrentLevel}, " +
                              $"Hệ số: {InnateMultiplier:P0}, " +
                              $"Cảnh giới: {GetRealmDisplayName(CurrentRealm)}, " +
                              $"HP: {HP}/{MaxHP}, Hồn Lực: {SoulPower}/{MaxSoulPower}");
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

            // Cập nhật hồi phục HP theo thời gian (HoT) từ Oscar Sausage
            if (HoTTimer > 0)
            {
                HoTTimer -= deltaTime;
                Heal(MaxHP * 0.05f * deltaTime); // Hồi 5% HP tối đa mỗi giây
            }

            switch (CurrentState)
            {
                case CultivationState.Idle:
                    // Không làm gì — chờ lệnh
                    break;

                case CultivationState.Meditating:
                    UpdateMeditating(deltaTime);
                    break;

                case CultivationState.BreakthroughReady:
                    // EXP bị khóa — chờ player đi săn Hồn Thú
                    break;

                case CultivationState.HuntingRing:
                    // Sẽ được xử lý bởi CombatSystem (chưa implement)
                    break;

                case CultivationState.AbsorbingRing:
                    UpdateAbsorbingRing(deltaTime);
                    break;

                case CultivationState.Dead:
                    // Không xử lý gì
                    break;
            }
        }

        // ====================================================================
        // STATE TRANSITIONS — Chuyển trạng thái
        // ====================================================================

        /// <summary>Bắt đầu Minh Tưởng (tu luyện).</summary>
        public void StartMeditating()
        {
            if (CurrentState == CultivationState.Dead)
            {
                Console.WriteLine($"[Tu Luyện] {OwnerName} đã chết, không thể tu luyện!");
                return;
            }

            if (CurrentState == CultivationState.BreakthroughReady)
            {
                Console.WriteLine($"[Tu Luyện] {OwnerName} đã chạm bình cảnh! " +
                                  "Cần Hồn Hoàn để đột phá, không thể Minh Tưởng tiếp.");
                return;
            }

            if (CurrentState == CultivationState.AbsorbingRing)
            {
                Console.WriteLine($"[Tu Luyện] {OwnerName} đang hấp thu Hồn Hoàn, không thể chuyển trạng thái!");
                return;
            }

            if (InnateMultiplier <= 0f)
            {
                Console.WriteLine($"[Tu Luyện] {OwnerName} là Phế Vũ Hồn — " +
                                  "không có Hồn Lực, không thể trở thành Hồn Sư!");
                return;
            }

            CurrentState = CultivationState.Meditating;
            Console.WriteLine($"[Tu Luyện] ★ {OwnerName} bắt đầu Minh Tưởng... " +
                              $"Hồn Lực hiện tại: Cấp {CurrentLevel} " +
                              $"({CurrentExp:F0}/{MaxExpForCurrentLevel:F0} EXP)");
        }

        /// <summary>Dừng Minh Tưởng, về trạng thái Idle.</summary>
        public void StopMeditating()
        {
            if (CurrentState == CultivationState.Meditating)
            {
                CurrentState = CultivationState.Idle;
                Console.WriteLine($"[Tu Luyện] {OwnerName} ngừng Minh Tưởng. " +
                                  $"EXP: {CurrentExp:F0}/{MaxExpForCurrentLevel:F0}");
            }
        }

        /// <summary>
        /// Bắt đầu hấp thu Hồn Hoàn. Chuyển FSM sang ABSORBING_RING.
        /// Chỉ có thể gọi khi đang ở trạng thái BREAKTHROUGH_READY.
        /// </summary>
        /// <param name="soulBeastAge">Tuổi Hồn Thú (năm tu vi) — quyết định rủi ro.</param>
        public void StartAbsorbingSoulRing(int soulBeastAge, SandboxTuTien.Core.Combat.Element element)
        {
            if (CurrentState != CultivationState.BreakthroughReady &&
                CurrentState != CultivationState.Idle)
            {
                Console.WriteLine($"[Hồn Hoàn] {OwnerName} chưa sẵn sàng hấp thu! " +
                                  $"Trạng thái hiện tại: {CurrentState}");
                return;
            }

            if (soulBeastAge <= 0)
            {
                Console.WriteLine($"[Hồn Hoàn] Tuổi Hồn Thú không hợp lệ: {soulBeastAge}");
                return;
            }

            _absorbingSoulBeastAge = soulBeastAge;
            _absorbingSoulBeastElement = element;
            _absorptionElapsed = 0f;
            _currentDamageWave = 0;
            _timeBetweenWaves = ABSORPTION_DURATION / ABSORPTION_DAMAGE_WAVES;
            _waveTimer = 0f;
            QTEPressCount = 0; // Reset số lần nhấn QTE

            CurrentState = CultivationState.AbsorbingRing;

            float successRate = CalculateAbsorptionSuccessRate(soulBeastAge);

            Console.WriteLine($"[Hồn Hoàn] ⚡ {OwnerName} bắt đầu hấp thu Hồn Hoàn!");
            Console.WriteLine($"           Hồn Thú: {soulBeastAge} năm tu vi (Hệ: {element})");
            Console.WriteLine($"           Cực hạn cơ thể: {BodyLimit:F0} năm");
            Console.WriteLine($"           Tỷ lệ thành công: {successRate:P1}");
            Console.WriteLine($"           Sẽ chịu {ABSORPTION_DAMAGE_WAVES} đợt sát thương nội tại...");
        }

        /// <summary>Thêm EXP trực tiếp (dùng cho debug/test).</summary>
        public void AddExp(float amount)
        {
            if (CurrentState == CultivationState.Dead)
            {
                Console.WriteLine($"[Tu Luyện] {OwnerName} đã chết!");
                return;
            }

            if (CurrentState == CultivationState.BreakthroughReady)
            {
                Console.WriteLine($"[Tu Luyện] {OwnerName} đã chạm bình cảnh! " +
                                  "EXP bị khóa, cần Hồn Hoàn.");
                return;
            }

            CurrentExp += amount;
            Console.WriteLine($"[Debug] +{amount:F0} EXP → {OwnerName}: " +
                              $"{CurrentExp:F0}/{MaxExpForCurrentLevel:F0}");

            CheckLevelUp();
        }

        // ====================================================================
        // PRIVATE — Logic Minh Tưởng
        // ====================================================================

        private void UpdateMeditating(float deltaTime)
        {
            // Tốc độ tăng EXP = Base × Hệ số Tiên Thiên × deltaTime
            float expGain = BASE_MEDITATION_RATE * InnateMultiplier * deltaTime;
            CurrentExp += expGain;

            // Throttled logging
            _logTimer += deltaTime;
            if (_logTimer >= LOG_INTERVAL)
            {
                _logTimer = 0f;
                Console.WriteLine($"[Minh Tưởng] {OwnerName} đang tu luyện... " +
                                  $"Cấp {CurrentLevel} — EXP: {CurrentExp:F1}/{MaxExpForCurrentLevel:F0} " +
                                  $"(+{expGain / deltaTime:F1}/s)");
            }

            CheckLevelUp();
        }

        private void CheckLevelUp()
        {
            while (CurrentExp >= MaxExpForCurrentLevel && CurrentLevel < MAX_LEVEL)
            {
                // Trừ EXP dư
                CurrentExp -= MaxExpForCurrentLevel;
                int oldLevel = CurrentLevel;
                CurrentLevel++;

                // Cập nhật HP và BodyLimit
                MaxHP = CalculateMaxHP(CurrentLevel);
                HP = MaxHP;
                MaxSoulPower = CalculateMaxSoulPower(CurrentLevel);
                SoulPower = MaxSoulPower;

                Console.WriteLine($"[TĂNG CẤP] ★★★ {OwnerName} Hồn Lực tăng lên Cấp {CurrentLevel}! ★★★");

                // Publish event tăng cấp
                _eventManager.Publish(new OnLevelUpEvent
                {
                    PlayerName = OwnerName,
                    OldLevel = oldLevel,
                    NewLevel = CurrentLevel
                });

                // Kiểm tra thay đổi cảnh giới
                var newRealm = GetRealmForLevel(CurrentLevel);
                if (newRealm != CurrentRealm)
                {
                    var oldRealm = CurrentRealm;
                    CurrentRealm = newRealm;

                    Console.WriteLine($"[CẢNH GIỚI] ✦✦✦ {OwnerName} đột phá thành " +
                                      $"{GetRealmDisplayName(newRealm)}! ✦✦✦");

                    _eventManager.Publish(new OnRealmChangedEvent
                    {
                        PlayerName = OwnerName,
                        OldRealm = GetRealmDisplayName(oldRealm),
                        NewRealm = GetRealmDisplayName(newRealm),
                        Level = CurrentLevel
                    });
                }

                // Kiểm tra bình cảnh (mỗi 10 cấp = cần Hồn Hoàn)
                if (CurrentLevel % 10 == 0 && CurrentLevel < MAX_LEVEL)
                {
                    CurrentState = CultivationState.BreakthroughReady;
                    CurrentExp = 0; // Khóa EXP

                    // Cập nhật Body Limit cho Hồn Hoàn tiếp theo
                    BodyLimit = CalculateBodyLimit(SoulRingsCount);

                    Console.WriteLine($"[BÌNH CẢNH] ⚠ {OwnerName} chạm bình cảnh tại Cấp {CurrentLevel}!");
                    Console.WriteLine($"            Cần tiêu diệt Hồn Thú để nhận Hồn Hoàn thứ {SoulRingsCount + 1}.");
                    Console.WriteLine($"            Cực hạn hấp thu: {BodyLimit:F0} năm tu vi.");

                    _eventManager.Publish(new OnBottleneckReachedEvent
                    {
                        PlayerName = OwnerName,
                        Level = CurrentLevel,
                        CurrentRealm = GetRealmDisplayName(CurrentRealm)
                    });

                    break; // Dừng tăng cấp tại bình cảnh
                }
            }
        }

        // ====================================================================
        // PRIVATE — Logic Hấp Thu Hồn Hoàn
        // ====================================================================

        private void UpdateAbsorbingRing(float deltaTime)
        {
            _absorptionElapsed += deltaTime;
            _waveTimer += deltaTime;

            // Mỗi đợt damage
            if (_waveTimer >= _timeBetweenWaves && _currentDamageWave < ABSORPTION_DAMAGE_WAVES)
            {
                _waveTimer = 0f;
                _currentDamageWave++;

                // Sát thương nội tại = năng lượng bạo liệt từ Hồn Hoàn
                float damagePerWave = CalculateAbsorptionDamage(_absorbingSoulBeastAge);
                HP -= damagePerWave;

                Console.WriteLine($"[Hấp Thu] Đợt {_currentDamageWave}/{ABSORPTION_DAMAGE_WAVES}: " +
                                  $"Năng lượng Hồn Hoàn xé rách kinh mạch! " +
                                  $"-{damagePerWave:F0} HP → HP: {HP:F0}/{MaxHP:F0}");

                // Kiểm tra tử vong
                if (HP <= 0)
                {
                    HP = 0;
                    CurrentState = CultivationState.Dead;

                    Console.WriteLine($"[TỬ VONG] ✗✗✗ {OwnerName} CƠ THỂ BẠO LIỆT! ✗✗✗");
                    Console.WriteLine($"          Không chịu nổi năng lượng Hồn Hoàn " +
                                      $"{_absorbingSoulBeastAge} năm tu vi.");

                    _eventManager.Publish(new OnBreakthroughFailedEvent
                    {
                        PlayerName = OwnerName,
                        Reason = $"Bạo thể khi hấp thu Hồn Hoàn {_absorbingSoulBeastAge} năm " +
                                 $"(vượt cực hạn {BodyLimit:F0} năm)",
                        SuccessRate = CalculateAbsorptionSuccessRate(_absorbingSoulBeastAge)
                    });

                    _eventManager.Publish(new OnPlayerDiedEvent
                    {
                        PlayerName = OwnerName,
                        CauseOfDeath = "Bạo thể khi hấp thu Hồn Hoàn — kinh mạch đứt nát"
                    });

                    return;
                }
            }

            // Hoàn tất hấp thu
            if (_currentDamageWave >= ABSORPTION_DAMAGE_WAVES && _absorptionElapsed >= ABSORPTION_DURATION)
            {
                CompleteAbsorption();
            }
        }

        private void CompleteAbsorption()
        {
            SoulRingsCount++;
            CurrentState = CultivationState.Meditating;
            CurrentExp = 0;
            BodyLimit = CalculateBodyLimit(SoulRingsCount);

            // Tỷ lệ 1% rơi ra Ngoại Phụ Hồn Cốt Bát Chu Mâu nếu hấp thu vượt BodyLimit
            if (_absorbingSoulBeastAge > BodyLimit && !HasBatChuMau)
            {
                if (_random.NextDouble() < 0.01)
                {
                    UnlockBatChuMau();
                }
            }

            // Hồi phục HP sau hấp thu thành công
            MaxHP = CalculateMaxHP(CurrentLevel);
            HP = MaxHP;
            MaxSoulPower = CalculateMaxSoulPower(CurrentLevel);
            SoulPower = MaxSoulPower;

            string ringColor = GetSoulRingColor(SoulRingsCount);

            // Mở khóa Hồn Kỹ tương ứng dựa theo Hệ của Hồn thú và thứ tự Hồn hoàn
            string skillGainedInfo = "Chưa gán";
            if (SoulRingsCount == 1)
            {
                Skill1 = _absorbingSoulBeastElement switch
                {
                    Core.Combat.Element.Wood => new SoulSkill("Lam Ngan Quan Quanh", Core.Combat.Element.Wood, 1, 20f, 60f),
                    Core.Combat.Element.Fire => new SoulSkill("Phuong Hoang Hoa Tuyen", Core.Combat.Element.Fire, 1, 25f, 100f),
                    Core.Combat.Element.Ice => new SoulSkill("Bang Tam Ket Gioi", Core.Combat.Element.Ice, 1, 30f, 80f),
                    _ => new SoulSkill("Huyen Thiet Kich", Core.Combat.Element.None, 1, 15f, 50f)
                };
                skillGainedInfo = Skill1.Name;
            }
            else if (SoulRingsCount == 2)
            {
                Skill2 = _absorbingSoulBeastElement switch
                {
                    Core.Combat.Element.Wood => new SoulSkill("Lam Ngan Tu Lung", Core.Combat.Element.Wood, 2, 40f, 150f),
                    Core.Combat.Element.Fire => new SoulSkill("Phuong Hoang Huyen Oa", Core.Combat.Element.Fire, 2, 50f, 200f),
                    Core.Combat.Element.Ice => new SoulSkill("Huyen Bang Xung Kich", Core.Combat.Element.Ice, 2, 45f, 160f),
                    _ => new SoulSkill("Chan Thien Than Quyen", Core.Combat.Element.None, 2, 35f, 120f)
                };
                skillGainedInfo = Skill2.Name;
            }

            Console.WriteLine($"[HỒN HOÀN] ★★★ {OwnerName} hấp thu Hồn Hoàn thứ {SoulRingsCount} " +
                              $"THÀNH CÔNG! ★★★");
            Console.WriteLine($"           Hồn Hoàn: {ringColor} — " +
                              $"Hồn Thú {_absorbingSoulBeastAge} năm tu vi (Hệ: {_absorbingSoulBeastElement})");
            Console.WriteLine($"           Cảnh giới: {GetRealmDisplayName(CurrentRealm)} " +
                              $"— Cấp {CurrentLevel}");
            Console.WriteLine($"           Hồn Kỹ mới: {skillGainedInfo}");

            // Tỷ lệ rớt Hồn Cốt: 1/1000
            bool droppedSoulBone = _random.Next(1000) == 0;
            if (droppedSoulBone)
            {
                Console.WriteLine($"[CỰC HIẾM] ✦✦✦ HỒN CỐT xuất hiện! " +
                                  $"{OwnerName} nhận được Soul Bone! ✦✦✦");
            }

            _eventManager.Publish(new OnBreakthroughSuccessEvent
            {
                PlayerName = OwnerName,
                NewRealm = GetRealmDisplayName(CurrentRealm),
                NewLevel = CurrentLevel,
                SoulRingNumber = SoulRingsCount
            });

            _eventManager.Publish(new OnSoulRingAbsorbedEvent
            {
                PlayerName = OwnerName,
                RingNumber = SoulRingsCount,
                SoulBeastAge = _absorbingSoulBeastAge,
                SoulSkillName = "[Chưa gán]"
            });

            // Tự động tiếp tục Minh Tưởng
            Console.WriteLine($"[Tu Luyện] {OwnerName} tiếp tục Minh Tưởng...");
        }

        /// <summary>
        /// Mở khóa Ngoại Phụ Hồn Cốt Bát Chu Mâu (tăng vĩnh viễn HP, SP và kích hoạt passive độc).
        /// </summary>
        public void UnlockBatChuMau()
        {
            if (HasBatChuMau) return;
            HasBatChuMau = true;
            MaxHP = CalculateMaxHP(CurrentLevel);
            HP = Math.Clamp(HP + 50f, 0f, MaxHP);
            MaxSoulPower = CalculateMaxSoulPower(CurrentLevel);
            SoulPower = Math.Clamp(SoulPower + 30f, 0f, MaxSoulPower);
            Console.WriteLine($"[CỰC HIẾM] ★★★ {OwnerName} đã hấp thu NGOẠI PHỤ HỒN CỐT BÁT CHU MÂU! (+50 MaxHP, +30 MaxSP, đòn đánh thường có 25% cơ hội tẩm độc) ★★★");
        }

        // ====================================================================
        // CALCULATIONS — Công thức tính toán
        // ====================================================================

        /// <summary>
        /// EXP cần để lên cấp = BASE × (1 + level × 0.5).
        /// Cấp càng cao càng cần nhiều EXP.
        /// </summary>
        private float CalculateExpRequired(int level)
        {
            return BASE_EXP_PER_LEVEL * (1f + level * 0.5f);
        }

        /// <summary>
        /// HP tối đa = 100 + level × 20 + 50 (nếu có Bát Chu Mâu).
        /// </summary>
        private float CalculateMaxHP(int level)
        {
            float baseHP = 100f + level * 20f;
            return HasBatChuMau ? baseHP + 50f : baseHP;
        }

        /// <summary>
        /// Hồn Lực năng lượng tối đa = 100 + level × 10 + 30 (nếu có Bát Chu Mâu).
        /// </summary>
        private float CalculateMaxSoulPower(int level)
        {
            float baseSP = 100f + level * 10f;
            return HasBatChuMau ? baseSP + 30f : baseSP;
        }

        /// <summary>
        /// Cực hạn chịu đựng cơ thể dựa trên số Hồn Hoàn đã hấp thu.
        /// Theo CONTEXT mục 2.B — giới hạn Hồn Hoàn:
        ///   Ring 1: 100-423 năm → limit ~423
        ///   Ring 2: ~760 năm
        ///   Ring 3: ~1700 năm
        ///   Ring 4: ~5000 năm
        ///   Ring 5: ~12000 năm
        ///   Ring 6: ~20000 năm
        ///   Ring 7: ~50000 năm
        ///   Ring 8-9: ~100000 năm
        /// </summary>
        private float CalculateBodyLimit(int currentRingCount)
        {
            int nextRing = currentRingCount + 1;
            return nextRing switch
            {
                1 => 423f,
                2 => 760f,
                3 => 1700f,
                4 => 5000f,
                5 => 12000f,
                6 => 20000f,
                7 => 50000f,
                8 => 100000f,
                9 => 100000f,
                _ => 100f
            };
        }

        /// <summary>
        /// Tỷ lệ thành công hấp thu Hồn Hoàn (theo CONTEXT mục 3.C):
        /// Tỷ_Lệ = (Body_Limit / Hồn_Hoàn_Age) × 100% + Willpower_Buff
        /// Clamp trong [0%, 100%].
        /// Nếu Body_Limit >= Hồn_Hoàn_Age → gần 100% (an toàn).
        /// Nếu Hồn_Hoàn_Age >> Body_Limit → rủi ro cực cao.
        /// </summary>
        private float CalculateAbsorptionSuccessRate(int soulBeastAge)
        {
            if (soulBeastAge <= 0) return 1f;
            float qteBuff = QTEPressCount * 0.02f; // Mỗi lần ấn Spacebar cộng 2% tỷ lệ thành công
            float rate = (BodyLimit / soulBeastAge) + WillpowerBuff + qteBuff;
            return Math.Clamp(rate, 0f, 1f);
        }

        /// <summary>
        /// Sát thương nội tại mỗi đợt khi hấp thu.
        /// Tỷ lệ nghịch với tỷ lệ thành công — Hồn Hoàn càng mạnh, damage càng lớn.
        /// Damage = MaxHP × (1 - SuccessRate) × (0.3 + random variance)
        /// </summary>
        private float CalculateAbsorptionDamage(int soulBeastAge)
        {
            float successRate = CalculateAbsorptionSuccessRate(soulBeastAge);
            float dangerFactor = 1f - successRate;
            float variance = 0.3f + (float)_random.NextDouble() * 0.2f; // 0.3 - 0.5
            return MaxHP * dangerFactor * variance;
        }

        // ====================================================================
        // STATIC HELPERS
        // ====================================================================

        /// <summary>Xác định cảnh giới dựa trên cấp Hồn Lực.</summary>
        public static CultivationRealm GetRealmForLevel(int level)
        {
            return level switch
            {
                >= 100 => CultivationRealm.Than,
                >= 91 => CultivationRealm.PhongHaoDauLa,
                >= 81 => CultivationRealm.HonDauLa,
                >= 71 => CultivationRealm.HonThanh,
                >= 61 => CultivationRealm.HonDe,
                >= 51 => CultivationRealm.HonVuong,
                >= 41 => CultivationRealm.HonTong,
                >= 31 => CultivationRealm.HonTon,
                >= 21 => CultivationRealm.DaiHonSu,
                >= 11 => CultivationRealm.HonSu,
                _ => CultivationRealm.HonSi
            };
        }

        /// <summary>Trả về tên hiển thị tiếng Việt của cảnh giới.</summary>
        public static string GetRealmDisplayName(CultivationRealm realm)
        {
            return realm switch
            {
                CultivationRealm.HonSi => "Hồn Sĩ",
                CultivationRealm.HonSu => "Hồn Sư",
                CultivationRealm.DaiHonSu => "Đại Hồn Sư",
                CultivationRealm.HonTon => "Hồn Tôn",
                CultivationRealm.HonTong => "Hồn Tông",
                CultivationRealm.HonVuong => "Hồn Vương",
                CultivationRealm.HonDe => "Hồn Đế",
                CultivationRealm.HonThanh => "Hồn Thánh",
                CultivationRealm.HonDauLa => "Hồn Đấu La",
                CultivationRealm.PhongHaoDauLa => "Phong Hào Đấu La",
                CultivationRealm.Than => "Thần",
                _ => "Không Rõ"
            };
        }

        /// <summary>Trả về màu Hồn Hoàn theo thứ tự (1-9).</summary>
        private static string GetSoulRingColor(int ringNumber)
        {
            return ringNumber switch
            {
                1 => "Bạch sắc (Trắng)",
                2 => "Hoàng sắc (Vàng)",
                3 => "Tử sắc (Tím)",
                4 => "Hắc sắc (Đen)",
                5 => "Hắc sắc (Đen)",
                6 => "Hắc sắc (Đen)",
                7 => "Hắc sắc (Đen)",
                8 => "Hồng sắc (Đỏ)",
                9 => "Hồng sắc (Đỏ)",
                _ => "Không Rõ"
            };
        }

        // ====================================================================
        // SERIALIZATION (Placeholder cho Save/Load System)
        // ====================================================================

        /// <summary>
        /// Trả về chuỗi JSON biểu diễn trạng thái hiện tại.
        /// Theo yêu cầu CONTEXT mục 5: Robust Save/Load System.
        /// </summary>
        public string Serialize()
        {
            var data = new
            {
                ownerName = OwnerName,
                currentLevel = CurrentLevel,
                currentExp = CurrentExp,
                innateMultiplier = InnateMultiplier,
                currentState = CurrentState.ToString(),
                currentRealm = CurrentRealm.ToString(),
                soulRingsCount = SoulRingsCount,
                hp = HP,
                maxHp = MaxHP,
                soulPower = SoulPower,
                maxSoulPower = MaxSoulPower,
                willpowerBuff = WillpowerBuff
            };

            return System.Text.Json.JsonSerializer.Serialize(data);
        }

        /// <summary>
        /// Khôi phục trạng thái tu vi từ hệ thống lưu trữ MySQL (Save/Load).
        /// </summary>
        public void LoadState(int level, float exp, float hp, float maxHp, float sp, float maxSp, int ringsCount, bool hasBatChuMau, string realmStr, string skill1Name, string skill2Name)
        {
            CurrentLevel = level;
            CurrentExp = exp;
            HP = hp;
            MaxHP = maxHp;
            SoulPower = sp;
            MaxSoulPower = maxSp;
            SoulRingsCount = ringsCount;
            HasBatChuMau = hasBatChuMau;
            BodyLimit = CalculateBodyLimit(SoulRingsCount);

            if (Enum.TryParse<CultivationRealm>(realmStr, out var realm))
            {
                CurrentRealm = realm;
            }
            else
            {
                CurrentRealm = GetRealmForLevel(CurrentLevel);
            }

            // Phục hồi các kỹ năng đã học dựa vào tên kỹ năng
            if (!string.IsNullOrEmpty(skill1Name))
            {
                if (skill1Name.Contains("Lam Ngan"))
                    Skill1 = new SoulSkill(skill1Name, Core.Combat.Element.Wood, 1, 20f, 60f);
                else if (skill1Name.Contains("Phuong Hoang"))
                    Skill1 = new SoulSkill(skill1Name, Core.Combat.Element.Fire, 1, 25f, 100f);
                else if (skill1Name.Contains("Bang"))
                    Skill1 = new SoulSkill(skill1Name, Core.Combat.Element.Ice, 1, 30f, 80f);
                else
                    Skill1 = new SoulSkill(skill1Name, Core.Combat.Element.None, 1, 15f, 50f);
            }
            else
            {
                Skill1 = null;
            }

            if (!string.IsNullOrEmpty(skill2Name))
            {
                if (skill2Name.Contains("Lam Ngan"))
                    Skill2 = new SoulSkill(skill2Name, Core.Combat.Element.Wood, 2, 40f, 150f);
                else if (skill2Name.Contains("Phuong Hoang"))
                    Skill2 = new SoulSkill(skill2Name, Core.Combat.Element.Fire, 2, 50f, 200f);
                else if (skill2Name.Contains("Bang"))
                    Skill2 = new SoulSkill(skill2Name, Core.Combat.Element.Ice, 2, 45f, 160f);
                else
                    Skill2 = new SoulSkill(skill2Name, Core.Combat.Element.None, 2, 35f, 120f);
            }
            else
            {
                Skill2 = null;
            }

            // Đưa trạng thái về Idle nếu không bị chết
            if (HP > 0 && CurrentState == CultivationState.Dead)
            {
                CurrentState = CultivationState.Idle;
            }
        }
    }
}
