using System;
using Microsoft.Xna.Framework;
using SandboxTuTien.Core.Combat;

namespace SandboxTuTien.Entities
{
    /// <summary>
    /// Thực thể Yêu Thú. Tuổi (năm tu hành) tăng dần theo thời gian,
    /// quyết định phẩm giai (Nhất → Cửu Giai), HP, kích thước và phẩm chất Yêu Đan rơi ra.
    /// </summary>
    public class Monster
    {
        public string BaseName { get; set; }

        public string Name => GetRankedName();

        public int Age { get; set; } // Số năm tu hành

        /// <summary>Phẩm giai Yêu Thú (1-9) suy ra từ số năm tu hành.</summary>
        public int Grade => GetGradeForAge(Age);

        public float HP { get; set; }
        public float MaxHP { get; set; }
        public float BaseMaxHP { get; set; } // HP gốc khi tạo Yêu Thú
        public Vector2 Position { get; set; }
        public Element Element { get; set; }
        public bool Active { get; set; }
        public float Radius { get; set; } // Bán kính va chạm (tự động tăng theo tuổi)

        // Các thuộc tính bổ sung nâng cao
        public string Role { get; set; } = "Magic"; // Tank, Speed, Magic
        public float RootTimer { get; set; } = 0f;
        public float PoisonTimer { get; set; } = 0f;
        public float PoisonTickTimer { get; set; } = 0f;
        public float PanicTimer { get; set; } = 0f;
        public Vector2 PanicSource { get; set; } = Vector2.Zero;
        public bool IsAggroed { get; set; } = false;

        private Vector2 _roamDir = Vector2.Zero;

        /// <summary>Phần lẻ số năm tích lũy giữa các frame (tránh bị cắt khi ép kiểu int).</summary>
        private float _ageAccumulator = 0f;

        private readonly Random _random = new();

        /// <summary>Sự kiện kích hoạt khi Yêu Thú chết (để rơi Yêu Đan).</summary>
        public event Action<Monster>? OnKilled;

        private static readonly string[] GradeNames =
        {
            "Nhất Giai", "Nhị Giai", "Tam Giai", "Tứ Giai", "Ngũ Giai",
            "Lục Giai", "Thất Giai", "Bát Giai", "Cửu Giai"
        };

        public Monster(string name, int age, float maxHp, Vector2 position, Element element)
        {
            BaseName = name;
            Age = age;
            BaseMaxHP = maxHp;
            MaxHP = maxHp * (1f + age / 1500f); // Tỷ lệ HP tăng theo tuổi ban đầu
            HP = MaxHP;
            Position = position;
            Element = element;
            Active = true;
            UpdateRadius();

            // Phân chia Role dựa vào hệ nguyên tố
            Role = element switch
            {
                Element.Wood => "Speed",
                Element.Fire => "Magic",
                Element.Ice => "Tank",
                _ => "Magic"
            };
        }

        /// <summary>
        /// Phẩm giai theo số năm tu hành: 100 / 300 / 1.000 / 3.000 / 10.000 / 30.000 / 100.000 / 300.000.
        /// </summary>
        public static int GetGradeForAge(int age)
        {
            return age switch
            {
                < 100 => 1,
                < 300 => 2,
                < 1000 => 3,
                < 3000 => 4,
                < 10000 => 5,
                < 30000 => 6,
                < 100000 => 7,
                < 300000 => 8,
                _ => 9
            };
        }

        /// <summary>Tên phẩm giai, VD: "Tam Giai".</summary>
        public static string GetGradeName(int grade)
        {
            return GradeNames[Math.Clamp(grade, 1, 9) - 1];
        }

        /// <summary>
        /// Cập nhật bán kính va chạm theo hàm logarit của tuổi thọ.
        /// </summary>
        private void UpdateRadius()
        {
            Radius = 16f + (float)Math.Log(Math.Max(1, Age), 10) * 4.5f;
        }

        /// <summary>
        /// Tên hiển thị có phẩm giai và vai trò, VD: "Tam Giai Hỏa Vân Lang [Pháp]".
        /// </summary>
        private string GetRankedName()
        {
            string roleName = Role switch
            {
                "Speed" => "Tốc",
                "Tank" => "Thủ",
                _ => "Pháp"
            };
            return $"{GetGradeName(Grade)} {BaseName} [{roleName}]";
        }

        /// <summary>
        /// Cập nhật tiến hóa và tăng tuổi thọ của Yêu Thú theo thời gian.
        /// </summary>
        public void UpdateEvolution(float deltaTime, float timeScale, Vector2 playerPos)
        {
            if (!Active) return;

            // 1. Cập nhật Trói chân (Root)
            if (RootTimer > 0)
            {
                RootTimer -= deltaTime;
            }

            // 2. Cập nhật Hoảng sợ (bị uy áp)
            if (PanicTimer > 0)
            {
                PanicTimer -= deltaTime;
                Vector2 fleeDir = Position - PanicSource;
                if (fleeDir != Vector2.Zero)
                {
                    fleeDir.Normalize();
                    Position += fleeDir * 120f * deltaTime; // Chạy nhanh
                    Position = new Vector2(
                        Math.Clamp(Position.X, 16f, 2000f - 16f),
                        Math.Clamp(Position.Y, 16f, 2000f - 16f)
                    );
                }
            }
            // Di chuyển tự do nếu không bị trói và không bị hoảng sợ
            else if (RootTimer <= 0)
            {
                if (IsAggroed)
                {
                    // Truy đuổi người chơi
                    Vector2 chaseDir = playerPos - Position;
                    if (chaseDir != Vector2.Zero)
                    {
                        chaseDir.Normalize();
                        Position += chaseDir * 50f * deltaTime;
                    }
                }
                else
                {
                    if (_random.NextDouble() < 0.02)
                    {
                        _roamDir = new Vector2((float)(_random.NextDouble() * 2 - 1), (float)(_random.NextDouble() * 2 - 1));
                        if (_roamDir != Vector2.Zero) _roamDir.Normalize();
                    }
                    Position += _roamDir * 25f * deltaTime;
                }
                Position = new Vector2(
                    Math.Clamp(Position.X, 16f, 2000f - 16f),
                    Math.Clamp(Position.Y, 16f, 2000f - 16f)
                );
            }

            // 3. Cập nhật Độc tố DoT
            if (PoisonTimer > 0)
            {
                PoisonTimer -= deltaTime;
                PoisonTickTimer += deltaTime;
                if (PoisonTickTimer >= 1.0f)
                {
                    PoisonTickTimer = 0f;
                    float poisonDmg = MaxHP * 0.02f; // Mất 2% máu tối đa mỗi giây
                    HP -= poisonDmg;
                    Console.WriteLine($"[Độc Tố] {Name} nhận {poisonDmg:F0} sát thương độc. HP còn: {HP:F0}/{MaxHP:F0}");

                    if (HP <= 0)
                    {
                        HP = 0;
                        Active = false;
                        Console.WriteLine($"[Độc Tố] ☠ {Name} đã gục ngã vì độc tố tích tụ!");
                        OnKilled?.Invoke(this);
                        return;
                    }
                }
            }
            else
            {
                PoisonTickTimer = 0f;
            }

            int oldAge = Age;

            // Tốc độ tăng trưởng tuổi: khoảng 8 đến 15 năm mỗi giây thực tế
            _ageAccumulator += (float)(_random.NextDouble() * 7.0 + 8.0) * deltaTime;
            int wholeYears = (int)_ageAccumulator;
            _ageAccumulator -= wholeYears;
            Age += wholeYears;

            // Cập nhật lại HP và kích thước nếu tuổi thay đổi
            if (Age != oldAge)
            {
                float oldMaxHP = MaxHP;
                MaxHP = BaseMaxHP * (1f + Age / 1500f);

                // Hồi phục HP theo tỷ lệ
                if (oldMaxHP > 0)
                {
                    HP = (HP / oldMaxHP) * MaxHP;
                }
                else
                {
                    HP = MaxHP;
                }

                UpdateRadius();

                int oldGrade = GetGradeForAge(oldAge);
                if (Grade != oldGrade)
                {
                    Console.WriteLine($"[Tiến Hóa] ✦ Yêu Thú {BaseName} tấn thăng: {GetGradeName(oldGrade)} → {GetGradeName(Grade)} ({Age} năm)!");
                }
            }
        }

        /// <summary>
        /// Nhận sát thương và tính toán khắc chế thuộc tính.
        /// </summary>
        public void TakeDamage(float baseDamage, Element attackElement, out float finalDamage, out bool isCounter)
        {
            isCounter = CheckCounter(attackElement, Element);
            finalDamage = isCounter ? baseDamage * 1.5f : baseDamage;

            // Hỏa thiêu tịnh độc tố
            if (attackElement == Element.Fire && PoisonTimer > 0)
            {
                PoisonTimer = 0f;
                Console.WriteLine($"[Hỏa Tịnh Độc] ★ Liệt hỏa thiêu rụi toàn bộ độc tố trên người {Name}!");
            }

            HP -= finalDamage;

            Console.WriteLine($"[Chiến Đấu] {Name} ({Element}) nhận {finalDamage:F0} sát thương hệ {attackElement}. " +
                              (isCounter ? "★ KHẮC HỆ (+50% sát thương)!" : "") +
                              $" HP còn: {HP:F0}/{MaxHP:F0}");

            if (HP <= 0)
            {
                HP = 0;
                Active = false;
                Console.WriteLine($"[Chiến Đấu] ☠ Yêu Thú {Name} ({Age} năm) đã bị trảm sát!");
                OnKilled?.Invoke(this);
            }
        }

        /// <summary>
        /// Kiểm tra vòng tròn khắc chế: Hỏa > Mộc > Băng > Hỏa.
        /// </summary>
        private bool CheckCounter(Element attacker, Element defender)
        {
            if (attacker == Element.None || defender == Element.None) return false;

            return (attacker == Element.Fire && defender == Element.Wood) ||
                   (attacker == Element.Wood && defender == Element.Ice) ||
                   (attacker == Element.Ice && defender == Element.Fire);
        }

        /// <summary>
        /// Kiểm tra va chạm với tia đạn bằng khoảng cách hình tròn.
        /// </summary>
        public bool CheckCollision(Vector2 point, float pointRadius = 2f)
        {
            if (!Active) return false;
            float distance = Vector2.Distance(Position, point);
            return distance <= (Radius + pointRadius);
        }
    }
}
