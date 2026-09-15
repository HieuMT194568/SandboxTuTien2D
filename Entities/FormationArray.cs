using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using SandboxTuTien.Core.Combat;

namespace SandboxTuTien.Entities
{
    /// <summary>
    /// Trận Pháp tự động công kích, cắm Trận Kỳ xuống đất để hộ thân.
    /// Mỗi đợt công kích tiêu hao linh lực tích trữ (nạp bằng Linh Thạch).
    /// Có 3 loại: Kim Châm Trận, Phá Quân Kiếm Trận, Vạn Độc Trận.
    /// </summary>
    public class FormationArray
    {
        public Vector2 Position { get; set; }
        public float CooldownTimer { get; set; }
        public float FireRate { get; set; }
        public float Range { get; set; }
        public bool Active { get; set; }

        public int FormationType { get; set; } // 1: Kim Châm Trận, 2: Phá Quân Kiếm Trận, 3: Vạn Độc Trận
        public int AmmoCount { get; set; }
        public int MaxAmmo { get; set; }

        public string Name => GetFormationName(FormationType);

        public FormationArray(Vector2 position, int formationType)
        {
            Position = position;
            FormationType = formationType;
            CooldownTimer = 0f;
            Active = true;

            switch (FormationType)
            {
                case 1: // Kim Châm Trận
                    FireRate = 0.5f;   // Phóng châm siêu nhanh
                    Range = 220f;
                    MaxAmmo = 20;
                    break;
                case 2: // Phá Quân Kiếm Trận
                    FireRate = 3.0f;   // Kiếm khí chậm nhưng cực mạnh
                    Range = 260f;
                    MaxAmmo = 15;
                    break;
                case 3: // Vạn Độc Trận
                default:
                    FireRate = 2.0f;
                    Range = 180f;
                    MaxAmmo = 15;
                    break;
            }
            AmmoCount = MaxAmmo;
        }

        /// <summary>Tên hiển thị của loại trận pháp.</summary>
        public static string GetFormationName(int formationType)
        {
            return formationType switch
            {
                1 => "Kim Châm Trận",
                2 => "Phá Quân Kiếm Trận",
                3 => "Vạn Độc Trận",
                _ => "Trận Pháp"
            };
        }

        /// <summary>
        /// Nạp đầy linh lực cho trận.
        /// </summary>
        public void Reload()
        {
            AmmoCount = MaxAmmo;
        }

        /// <summary>
        /// Cập nhật quét mục tiêu và tự động công kích theo loại trận.
        /// </summary>
        public void Update(float deltaTime, List<Monster> monsters, ProjectilePool pool)
        {
            if (!Active) return;

            if (CooldownTimer > 0)
            {
                CooldownTimer -= deltaTime;
            }

            // Cạn linh lực thì trận ngừng vận chuyển
            if (AmmoCount <= 0)
            {
                return;
            }

            // Tìm Yêu Thú gần nhất trong tầm
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

                    if (FormationType == 1)
                    {
                        // Sát thương 10, bay nhanh, không kinh động Yêu Thú (Silent)
                        pool.Spawn(Position, dir, 10f, Range, 450f, Element.None, isSilent: true);
                        AmmoCount -= 1;
                        CooldownTimer = FireRate;
                        Console.WriteLine($"[Trận Pháp] ⚙ {Name} tại {Position.X:F0},{Position.Y:F0} phóng châm vào {target.Name}. Linh lực: {AmmoCount}/{MaxAmmo}");
                    }
                    else if (FormationType == 2)
                    {
                        // Kiếm khí bạo kích 80, tiêu hao 2 phần linh lực
                        if (AmmoCount >= 2)
                        {
                            pool.Spawn(Position, dir, 80f, Range, 400f, Element.None, isSilent: false);
                            AmmoCount -= 2;
                            CooldownTimer = FireRate;
                            Console.WriteLine($"[Trận Pháp] ⚙ {Name} tại {Position.X:F0},{Position.Y:F0} chém kiếm khí vào {target.Name}! Linh lực: {AmmoCount}/{MaxAmmo}");
                        }
                    }
                    else if (FormationType == 3)
                    {
                        // Vạn Độc Trận: phun độc vụ diện rộng (AoE)
                        pool.Spawn(Position, dir, 15f, Range, 300f, Element.Wood, isSilent: false);
                        AmmoCount -= 1;
                        CooldownTimer = FireRate;

                        // Đầu độc toàn bộ Yêu Thú trong phạm vi 80px quanh mục tiêu
                        foreach (var m in monsters)
                        {
                            if (m.Active && Vector2.Distance(target.Position, m.Position) <= 80f)
                            {
                                m.PoisonTimer = 5.0f;
                                m.IsAggroed = true;
                            }
                        }
                        Console.WriteLine($"[Trận Pháp] ⚙ {Name} tại {Position.X:F0},{Position.Y:F0} phun độc vụ lên {target.Name}! Linh lực: {AmmoCount}/{MaxAmmo}");
                    }
                }
            }
        }
    }
}
