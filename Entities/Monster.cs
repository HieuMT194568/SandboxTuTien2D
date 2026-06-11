using System;
using Microsoft.Xna.Framework;
using SandboxTuTien.Core.Combat;

namespace SandboxTuTien.Entities
{
    /// <summary>
    /// Thực thể Hồn Thú (Quái vật) phục vụ kiểm thử hệ thống chiến đấu.
    /// </summary>
    public class Monster
    {
        public string BaseName { get; set; }
        
        public string Name => GetRankedName();
        
        public int Age { get; set; } // Số năm tu vi
        public float HP { get; set; }
        public float MaxHP { get; set; }
        public float BaseMaxHP { get; set; } // HP gốc khi tạo quái
        public Vector2 Position { get; set; }
        public Element Element { get; set; }
        public bool Active { get; set; }
        public float Radius { get; set; } // Bán kính va chạm (sẽ tự động tăng theo tuổi)

        // Các thuộc tính bổ sung nâng cao
        public string Role { get; set; } = "Magic"; // Tank, Speed, Magic
        public float RootTimer { get; set; } = 0f;
        public float PoisonTimer { get; set; } = 0f;
        public float PoisonTickTimer { get; set; } = 0f;
        public float PanicTimer { get; set; } = 0f;
        public Vector2 PanicSource { get; set; } = Vector2.Zero;
        public bool IsAggroed { get; set; } = false;

        private Vector2 _roamDir = Vector2.Zero;
        private readonly Random _random = new();

        /// <summary>Sự kiện kích hoạt khi Hồn Thú chết (để tạo Hồn Hoàn).</summary>
        public event Action<Monster>? OnKilled;

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
        /// Cập nhật bán kính va chạm theo hàm logarit của tuổi thọ.
        /// </summary>
        private void UpdateRadius()
        {
            Radius = 16f + (float)Math.Log(Math.Max(1, Age), 10) * 4.5f;
        }

        /// <summary>
        /// Tạo tên có tiền tố cảnh giới theo tuổi thọ (Đấu La Đại Lục).
        /// </summary>
        private string GetRankedName()
        {
            string prefix = Age switch
            {
                < 100 => "Thap Nien",
                < 1000 => "Bach Nien",
                < 10000 => "Thien Nien",
                < 100000 => "Van Nien",
                _ => "Muoi Van Nien"
            };
            return $"{prefix} {BaseName} [{Role}]";
        }

        /// <summary>
        /// Cập nhật tiến hóa và tăng tuổi thọ của Hồn thú theo thời gian.
        /// </summary>
        public void UpdateEvolution(float deltaTime, float timeScale, Vector2 playerPos)
        {
            if (!Active) return;

            // 1. Cập nhật Trói chân (Root)
            if (RootTimer > 0)
            {
                RootTimer -= deltaTime;
            }

            // 2. Cập nhật Hoảng sợ (Panic/Fear)
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
                    // Chạy đuổi theo người chơi
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
                    Console.WriteLine($"[Độc Tố] {Name} nhận {poisonDmg:F0} sát thương độc DoT. HP còn: {HP:F0}/{MaxHP:F0}");
                    
                    if (HP <= 0)
                    {
                        HP = 0;
                        Active = false;
                        Console.WriteLine($"[Độc Tố] ☠ {Name} đã gục ngã vì trúng độc tố tích tụ!");
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
            float ageIncrease = (float)(_random.NextDouble() * 7.0 + 8.0) * deltaTime;
            Age = (int)(Age + ageIncrease);

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

                // Kiểm tra xem quái vật có đột phá tiền tố cảnh giới không
                string oldPrefix = oldAge switch { < 100 => "Thap Nien", < 1000 => "Bach Nien", < 10000 => "Thien Nien", < 100000 => "Van Nien", _ => "Muoi Van Nien" };
                string newPrefix = Age switch { < 100 => "Thap Nien", < 1000 => "Bach Nien", < 10000 => "Thien Nien", < 100000 => "Van Nien", _ => "Muoi Van Nien" };
                
                if (oldPrefix != newPrefix)
                {
                    Console.WriteLine($"[Tiến Hóa] ✦ Hồn Thú {BaseName} đã tiến hóa đột phá thành công: {oldPrefix} → {newPrefix} ({Age} năm)!");
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

            // Xử lý phản ứng thiêu đốt giải độc (Fire purge)
            if (attackElement == Element.Fire && PoisonTimer > 0)
            {
                PoisonTimer = 0f;
                Console.WriteLine($"[Hỏa Giải Độc] ★ Ngọn lửa bốc cháy dữ dội tiêu hủy toàn bộ chất độc trên cơ thể {Name}!");
            }

            HP -= finalDamage;

            Console.WriteLine($"[Chiến Đấu] {Name} ({Element}) nhận {finalDamage:F0} sát thương từ đạn hệ {attackElement}. " +
                              (isCounter ? "★ KHẮC HỆ (+50% Sát thương)!" : "") +
                              $" HP còn: {HP:F0}/{MaxHP:F0}");

            if (HP <= 0)
            {
                HP = 0;
                Active = false;
                Console.WriteLine($"[Chiến Đấu] ☠ Hồn Thú {Name} ({Age} năm) đã bị tiêu diệt!");
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
