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
                _ => "Van Nien"
            };
            return $"{prefix} {BaseName}";
        }

        /// <summary>
        /// Cập nhật tiến hóa và tăng tuổi thọ của Hồn thú theo thời gian.
        /// </summary>
        public void UpdateEvolution(float deltaTime, float timeScale)
        {
            if (!Active) return;

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
                string oldPrefix = oldAge switch { < 100 => "Thap Nien", < 1000 => "Bach Nien", < 10000 => "Thien Nien", _ => "Van Nien" };
                string newPrefix = Age switch { < 100 => "Thap Nien", < 1000 => "Bach Nien", < 10000 => "Thien Nien", _ => "Van Nien" };
                
                if (oldPrefix != newPrefix)
                {
                    Console.WriteLine($"[Tiến Hóa] ✦ Hồn Thú {BaseName} đã tiến hóa đột phá thành công: {oldPrefix} → {newPrefix} ({Age} năm)!");
                }
            }
        }

        /// <summary>
        /// Nhận sát thương và tính toán khắc chế thuộc tính.
        /// </summary>
        /// <param name="baseDamage">Sát thương cơ bản từ đạn.</param>
        /// <param name="attackElement">Hệ thuộc tính của đạn.</param>
        /// <param name="finalDamage">Sát thương thực tế sau tính khắc hệ.</param>
        /// <param name="isCounter">True nếu đạn khắc hệ quái vật.</param>
        public void TakeDamage(float baseDamage, Element attackElement, out float finalDamage, out bool isCounter)
        {
            isCounter = CheckCounter(attackElement, Element);
            finalDamage = isCounter ? baseDamage * 1.5f : baseDamage;

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
