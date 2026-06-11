using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using SandboxTuTien.Core.Combat;

namespace SandboxTuTien.Entities
{
    /// <summary>
    /// Bệ Phóng Ám Khí Tự Động (Auto-Turret) đặt xuống đất để bảo vệ nhân vật.
    /// Có 3 loại cơ quan: Vô Thanh Tụ Tiễn, Chư Cát Thần Nỗ, Hàm Sa Xạ Ảnh.
    /// </summary>
    public class AutoLauncher
    {
        public Vector2 Position { get; set; }
        public float CooldownTimer { get; set; }
        public float FireRate { get; set; }
        public float Range { get; set; }
        public bool Active { get; set; }

        public int TurretType { get; set; } // 1: Vô Thanh Tụ Tiễn, 2: Chư Cát Thần Nỗ, 3: Hàm Sa Xạ Ảnh
        public int AmmoCount { get; set; }
        public int MaxAmmo { get; set; }

        public AutoLauncher(Vector2 position, int turretType)
        {
            Position = position;
            TurretType = turretType;
            CooldownTimer = 0f;
            Active = true;

            // Cấu hình thông số riêng cho từng loại cơ quan
            switch (TurretType)
            {
                case 1: // Vô Thanh Tụ Tiễn
                    FireRate = 0.5f;   // Bắn siêu nhanh
                    Range = 220f;
                    MaxAmmo = 20;
                    break;
                case 2: // Chư Cát Thần Nỗ
                    FireRate = 3.0f;   // Bắn siêu chậm
                    Range = 260f;
                    MaxAmmo = 15;
                    break;
                case 3: // Hàm Sa Xạ Ảnh
                default:
                    FireRate = 2.0f;   // Tốc độ bắn vừa
                    Range = 180f;
                    MaxAmmo = 15;
                    break;
            }
            AmmoCount = MaxAmmo;
        }

        /// <summary>
        /// Nạp lại đầy đạn dược.
        /// </summary>
        public void Reload()
        {
            AmmoCount = MaxAmmo;
        }

        /// <summary>
        /// Cập nhật quét mục tiêu và tự động nã đạn dựa vào loại Turret.
        /// </summary>
        public void Update(float deltaTime, List<Monster> monsters, ProjectilePool pool)
        {
            if (!Active) return;

            // Đếm ngược thời gian hồi
            if (CooldownTimer > 0)
            {
                CooldownTimer -= deltaTime;
            }

            // Hết đạn không thể bắn
            if (AmmoCount <= 0)
            {
                return;
            }

            // Tìm quái gần nhất trong tầm bắn
            var target = monsters
                .Where(m => m.Active && Vector2.Distance(Position, m.Position) <= Range)
                .OrderBy(m => Vector2.Distance(Position, m.Position))
                .FirstOrDefault();

            if (target != null && CooldownTimer <= 0)
            {
                Vector2 dir = target.Position - Position;
                if (dir != Vector2.Zero)
                {
                    dir.Normalize();

                    if (TurretType == 1)
                    {
                        // Sát thương 10, bay nhanh, không Aggro quái (Silent)
                        pool.Spawn(Position, dir, 10f, Range, 450f, Element.None, isSilent: true);
                        AmmoCount -= 1;
                        CooldownTimer = FireRate;
                        Console.WriteLine($"[Cơ Quan] ⚙ Bệ phóng Vô Thanh Tụ Tiễn tại {Position.X:F0},{Position.Y:F0} bắn bắn tỉa {target.Name} (Không Aggro). Đạn: {AmmoCount}/{MaxAmmo}");
                    }
                    else if (TurretType == 2)
                    {
                        // Sát thương bạo kích 80, tốn 2 đạn mỗi viên nạp
                        if (AmmoCount >= 2)
                        {
                            pool.Spawn(Position, dir, 80f, Range, 400f, Element.None, isSilent: false);
                            AmmoCount -= 2;
                            CooldownTimer = FireRate;
                            Console.WriteLine($"[Cơ Quan] ⚙ Bệ phóng Chư Cát Thần Nỗ tại {Position.X:F0},{Position.Y:F0} đại pháo xuyên giáp {target.Name}! Đạn: {AmmoCount}/{MaxAmmo}");
                        }
                    }
                    else if (TurretType == 3)
                    {
                        // Hàm Sa Xạ Ảnh: Phun sương độc hại diện rộng (AoE) gây nhiễm độc cho quái xung quanh
                        pool.Spawn(Position, dir, 15f, Range, 300f, Element.Wood, isSilent: false);
                        AmmoCount -= 1;
                        CooldownTimer = FireRate;

                        // Đầu độc toàn bộ quái trong phạm vi 80px từ quái mục tiêu
                        foreach (var m in monsters)
                        {
                            if (m.Active && Vector2.Distance(target.Position, m.Position) <= 80f)
                            {
                                m.PoisonTimer = 5.0f; // Dính độc DoT 5s
                                m.IsAggroed = true;   // Làm tức giận quái
                            }
                        }
                        Console.WriteLine($"[Cơ Quan] ⚙ Bệ phóng Hàm Sa Xạ Ảnh tại {Position.X:F0},{Position.Y:F0} phun độc AoE lên {target.Name}! Đạn: {AmmoCount}/{MaxAmmo}");
                    }
                }
            }
        }
    }
}
