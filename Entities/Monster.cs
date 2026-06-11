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
        public string Name { get; set; }
        public int Age { get; set; } // Tuổi Hồn Thú (năm tu vi)
        public float HP { get; set; }
        public float MaxHP { get; set; }
        public Vector2 Position { get; set; }
        public Element Element { get; set; }
        public bool Active { get; set; }
        public float Radius { get; set; } = 20f; // Bán kính va chạm

        /// <summary>Sự kiện kích hoạt khi Hồn Thú chết (để tạo Hồn Hoàn).</summary>
        public event Action<Monster>? OnKilled;

        public Monster(string name, int age, float maxHp, Vector2 position, Element element)
        {
            Name = name;
            Age = age;
            MaxHP = maxHp;
            HP = maxHp;
            Position = position;
            Element = element;
            Active = true;
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
