using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using SandboxTuTien.Core.Combat;

namespace SandboxTuTien.Entities
{
    /// <summary>
    /// Bệ Phóng Ám Khí Tự Động (Auto-Turret) đặt xuống đất để bảo vệ nhân vật.
    /// Thiết kế theo mô hình Sandbox tự động hóa cơ quan.
    /// </summary>
    public class AutoLauncher
    {
        public Vector2 Position { get; set; }
        public float CooldownTimer { get; set; }
        public float FireRate { get; set; } = 1.5f; // Bắn mỗi 1.5 giây
        public float Range { get; set; } = 220f;     // Tầm quét bắn (pixel)
        public bool Active { get; set; }

        public AutoLauncher(Vector2 position)
        {
            Position = position;
            CooldownTimer = 0f;
            Active = true;
        }

        /// <summary>
        /// Cập nhật quét mục tiêu và tự động nã đạn.
        /// </summary>
        public void Update(float deltaTime, List<Monster> monsters, ProjectilePool pool)
        {
            if (!Active) return;

            // Đếm ngược thời gian hồi
            if (CooldownTimer > 0)
            {
                CooldownTimer -= deltaTime;
            }

            // Tìm quái gần nhất trong tầm bắn
            var target = monsters
                .Where(m => m.Active && Vector2.Distance(Position, m.Position) <= Range)
                .OrderBy(m => Vector2.Distance(Position, m.Position))
                .FirstOrDefault();

            if (target != null && CooldownTimer <= 0)
            {
                // Bắn đạn về phía quái vật
                Vector2 dir = target.Position - Position;
                if (dir != Vector2.Zero)
                {
                    dir.Normalize();
                    
                    // Sát thương 30, tầm bắn 220px, tốc độ bay 350px/s, hệ thường (None)
                    pool.Spawn(Position, dir, 30f, Range, 350f, Element.None);
                    CooldownTimer = FireRate;

                    Console.WriteLine($"[Cơ Quan] ⚙ Bệ phóng tại {Position.X:F0},{Position.Y:F0} bắn ám khí vào Hồn Thú {target.Name}!");
                }
            }
        }
    }
}
