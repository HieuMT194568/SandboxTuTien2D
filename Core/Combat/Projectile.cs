using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SandboxTuTien.Core.Combat
{
    /// <summary>
    /// Hệ nguyên tố trong game để tính khắc chế.
    /// Vòng tròn 3 hệ: Hỏa (Fire) > Mộc (Wood) > Băng (Ice) > Hỏa (Fire).
    /// </summary>
    public enum Element
    {
        None,
        Fire,   // Hỏa
        Wood,   // Mộc
        Ice     // Băng
    }

    /// <summary>Nguồn phóng ra tia đạn (để áp dụng nội tại/bonus của người chơi).</summary>
    public enum ProjectileOwner
    {
        Player,
        Formation
    }

    /// <summary>Tiện ích cho Element: đọc từ chuỗi JSON và tên hiển thị.</summary>
    public static class ElementExtensions
    {
        /// <summary>Chuyển chuỗi (VD: "Fire") thành Element, không khớp thì trả về None.</summary>
        public static Element ParseElement(string? value)
        {
            return Enum.TryParse<Element>(value, true, out var element) ? element : Element.None;
        }

        /// <summary>Tên hệ tiếng Việt.</summary>
        public static string GetDisplayName(this Element element)
        {
            return element switch
            {
                Element.Fire => "Hỏa",
                Element.Wood => "Mộc",
                Element.Ice => "Băng",
                _ => "Vô"
            };
        }
    }

    /// <summary>
    /// Một tia đạn đại diện cho phi kiếm / pháp thuật phóng ra.
    /// Quản lý bởi ProjectilePool để tránh phân bổ bộ nhớ liên tục.
    /// </summary>
    public class Projectile
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Damage { get; set; }
        public float Range { get; set; }
        public float DistanceTraveled { get; set; }
        public Element Element { get; set; }
        public bool Active { get; set; }
        public bool IsSilent { get; set; }
        public ProjectileOwner Owner { get; set; }

        /// <summary>Hiệu ứng trạng thái áp lên mục tiêu khi trúng.</summary>
        public IReadOnlyList<OnHitEffect> OnHitEffects { get; set; } = Array.Empty<OnHitEffect>();

        public Projectile()
        {
            Active = false;
        }

        /// <summary>
        /// Kích hoạt lại đạn.
        /// </summary>
        public void Spawn(Vector2 position, Vector2 velocity, float damage, float range, Element element,
                          bool isSilent, IReadOnlyList<OnHitEffect>? onHitEffects, ProjectileOwner owner)
        {
            Position = position;
            Velocity = velocity;
            Damage = damage;
            Range = range;
            DistanceTraveled = 0f;
            Element = element;
            IsSilent = isSilent;
            OnHitEffects = onHitEffects ?? Array.Empty<OnHitEffect>();
            Owner = owner;
            Active = true;
        }

        /// <summary>
        /// Cập nhật di chuyển của đạn.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!Active) return;

            Vector2 movement = Velocity * deltaTime;
            Position += movement;
            DistanceTraveled += movement.Length();

            if (DistanceTraveled >= Range)
            {
                Active = false;
            }
        }
    }
}
