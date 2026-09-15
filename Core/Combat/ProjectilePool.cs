using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace SandboxTuTien.Core.Combat
{
    /// <summary>
    /// Object Pool cho Projectiles để tránh quá tải GC (Garbage Collector).
    /// </summary>
    public class ProjectilePool
    {
        private readonly List<Projectile> _pool = new();
        private const int INITIAL_POOL_SIZE = 150;

        public IReadOnlyList<Projectile> Projectiles => _pool;

        public ProjectilePool()
        {
            // Cấp phát trước bộ đệm
            for (int i = 0; i < INITIAL_POOL_SIZE; i++)
            {
                _pool.Add(new Projectile());
            }
        }

        /// <summary>
        /// Kích hoạt một tia đạn từ pool.
        /// </summary>
        /// <param name="position">Vị trí bắt đầu bắn.</param>
        /// <param name="direction">Hướng bắn (đã chuẩn hóa).</param>
        /// <param name="damage">Sát thương đòn đánh.</param>
        /// <param name="range">Tầm bắn.</param>
        /// <param name="speed">Tốc độ bay.</param>
        /// <param name="element">Thuộc tính đạn.</param>
        /// <param name="isSilent">True nếu không kinh động Yêu Thú.</param>
        /// <param name="onHitEffects">Hiệu ứng trạng thái áp lên mục tiêu khi trúng.</param>
        /// <param name="owner">Nguồn phóng.</param>
        public void Spawn(Vector2 position, Vector2 direction, float damage, float range, float speed, Element element,
                          bool isSilent = false, IReadOnlyList<OnHitEffect>? onHitEffects = null,
                          ProjectileOwner owner = ProjectileOwner.Player)
        {
            // Tìm đạn đang không hoạt động
            var projectile = _pool.FirstOrDefault(p => !p.Active);

            if (projectile == null)
            {
                // Nếu hết đạn, cấp phát thêm để tránh lỗi
                projectile = new Projectile();
                _pool.Add(projectile);
            }

            projectile.Spawn(position, direction * speed, damage, range, element, isSilent, onHitEffects, owner);
        }

        /// <summary>
        /// Cập nhật tất cả đạn đang bay.
        /// </summary>
        public void Update(float deltaTime)
        {
            foreach (var proj in _pool)
            {
                if (proj.Active)
                {
                    proj.Update(deltaTime);
                }
            }
        }

        /// <summary>
        /// Vô hiệu hóa tất cả đạn (khi reset/clear).
        /// </summary>
        public void Clear()
        {
            foreach (var proj in _pool)
            {
                proj.Active = false;
            }
        }
    }
}
