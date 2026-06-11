using System;
using Microsoft.Xna.Framework;
using SandboxTuTien.Core.Combat;

namespace SandboxTuTien.Entities
{
    /// <summary>
    /// Thực thể Hồn Hoàn trôi nổi trên mặt đất chờ người chơi hấp thu.
    /// </summary>
    public class SoulRingEntity
    {
        public Vector2 Position { get; set; }
        public int Age { get; set; } // Số năm tu vi
        public Element Element { get; set; } // Hệ thuộc tính di truyền từ Hồn thú
        public bool Active { get; set; }
        public float Radius { get; set; } = 40f; // Khoảng cách hấp thu
        
        /// <summary>Biến đếm thời gian phục vụ hiệu ứng nhấp nháy/xoay.</summary>
        public float PulseTimer { get; set; }

        public SoulRingEntity(Vector2 position, int age, Element element)
        {
            Position = position;
            Age = age;
            Element = element;
            Active = true;
            PulseTimer = 0f;
        }

        public void Update(GameTime gameTime)
        {
            if (!Active) return;
            PulseTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        /// <summary>
        /// Trả về chuỗi mô tả màu sắc Hồn Hoàn dựa trên năm tu vi (Đấu La Đại Lục).
        /// </summary>
        public string GetColorName()
        {
            if (Age < 100) return "Bạch sắc (Trắng)";
            if (Age < 1000) return "Hoàng sắc (Vàng)";
            if (Age < 10000) return "Tử sắc (Tím)";
            if (Age < 100000) return "Hắc sắc (Đen)";
            return "Hồng sắc (Đỏ)";
        }

        /// <summary>
        /// Trả về màu vẽ Color thực tế trong game tương ứng với màu Hồn Hoàn.
        /// </summary>
        public Color GetColor()
        {
            if (Age < 100) return Color.White;
            if (Age < 1000) return Color.Gold;
            if (Age < 10000) return Color.Purple;
            if (Age < 100000) return new Color(25, 25, 25); // Đen huyền bí
            return Color.Red;
        }
    }
}
