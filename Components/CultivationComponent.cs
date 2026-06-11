using System;
using Microsoft.Xna.Framework;
using SandboxTuTien.Core;

namespace SandboxTuTien.Components
{
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

        /// <summary>Hồi HP (không vượt quá MaxHP).</summary>
        public void Heal(float amount)
        {
            if (CurrentState == CultivationState.Dead) return;
            HP = Math.Clamp(HP + amount, 0f, MaxHP);
            Console.WriteLine($"[Hồi HP] {OwnerName} được hồi {amount:F0} HP → HP: {HP:F0}/{MaxHP:F0}");
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
        public void StartAbsorbingSoulRing(int soulBeastAge)
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
            _absorptionElapsed = 0f;
            _currentDamageWave = 0;
            _timeBetweenWaves = ABSORPTION_DURATION / ABSORPTION_DAMAGE_WAVES;
            _waveTimer = 0f;

            CurrentState = CultivationState.AbsorbingRing;

            float successRate = CalculateAbsorptionSuccessRate(soulBeastAge);

            Console.WriteLine($"[Hồn Hoàn] ⚡ {OwnerName} bắt đầu hấp thu Hồn Hoàn!");
            Console.WriteLine($"           Hồn Thú: {soulBeastAge} năm tu vi");
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

            // Hồi phục HP sau hấp thu thành công
            MaxHP = CalculateMaxHP(CurrentLevel);
            HP = MaxHP;
            MaxSoulPower = CalculateMaxSoulPower(CurrentLevel);
            SoulPower = MaxSoulPower;

            string ringColor = GetSoulRingColor(SoulRingsCount);

            Console.WriteLine($"[HỒN HOÀN] ★★★ {OwnerName} hấp thu Hồn Hoàn thứ {SoulRingsCount} " +
                              $"THÀNH CÔNG! ★★★");
            Console.WriteLine($"           Hồn Hoàn: {ringColor} — " +
                              $"Hồn Thú {_absorbingSoulBeastAge} năm tu vi");
            Console.WriteLine($"           Cảnh giới: {GetRealmDisplayName(CurrentRealm)} " +
                              $"— Cấp {CurrentLevel}");
            Console.WriteLine($"           Hồn Kỹ mới: [Chưa gán — cần HồnKỹ DataSystem]");

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
        /// HP tối đa = 100 + level × 20.
        /// </summary>
        private float CalculateMaxHP(int level)
        {
            return 100f + level * 20f;
        }

        /// <summary>
        /// Hồn Lực năng lượng tối đa = 100 + level × 10.
        /// </summary>
        private float CalculateMaxSoulPower(int level)
        {
            return 100f + level * 10f;
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
            float rate = (BodyLimit / soulBeastAge) + WillpowerBuff;
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
    }
}
