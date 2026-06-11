using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SandboxTuTien.Systems
{
    /// <summary>
    /// Hệ thống quản lý tất cả CultivationComponent trong game.
    /// Tuân thủ kiến trúc ECS-lite: System lặp qua tất cả Components
    /// và gọi Update mỗi frame.
    ///
    /// Theo CONTEXT mục 5 (Time-Slicing Manager): 
    /// Chỉ xử lý tối đa MAX_UPDATES_PER_FRAME component mỗi frame
    /// để tránh lag spike khi có nhiều NPC.
    /// </summary>
    public class CultivationSystem
    {
        /// <summary>
        /// Số component tối đa được xử lý mỗi frame.
        /// Theo CONTEXT: chỉ 10 NPC được tính toán logic mỗi frame.
        /// </summary>
        private const int MAX_UPDATES_PER_FRAME = 10;

        /// <summary>Danh sách tất cả component đã đăng ký.</summary>
        private readonly List<Components.CultivationComponent> _components = new();

        /// <summary>Chỉ số bắt đầu cho round-robin time-slicing.</summary>
        private int _updateStartIndex;

        // ====================================================================
        // REGISTRATION
        // ====================================================================

        /// <summary>Đăng ký một CultivationComponent vào hệ thống.</summary>
        public void RegisterComponent(Components.CultivationComponent component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (!_components.Contains(component))
            {
                _components.Add(component);
                Console.WriteLine($"[CultivationSystem] Đã đăng ký: {component.OwnerName} " +
                                  $"(Tổng: {_components.Count})");
            }
        }

        /// <summary>Hủy đăng ký component khỏi hệ thống.</summary>
        public void UnregisterComponent(Components.CultivationComponent component)
        {
            if (_components.Remove(component))
            {
                Console.WriteLine($"[CultivationSystem] Đã hủy đăng ký: {component.OwnerName} " +
                                  $"(Còn lại: {_components.Count})");
            }
        }

        // ====================================================================
        // UPDATE — Time-Slicing
        // ====================================================================

        /// <summary>
        /// Cập nhật các component theo time-slicing.
        /// Mỗi frame chỉ xử lý tối đa MAX_UPDATES_PER_FRAME component,
        /// vòng lặp round-robin đảm bảo tất cả đều được xử lý công bằng.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (_components.Count == 0) return;

            int count = _components.Count;
            int updatesThisFrame = Math.Min(count, MAX_UPDATES_PER_FRAME);

            for (int i = 0; i < updatesThisFrame; i++)
            {
                int index = (_updateStartIndex + i) % count;
                _components[index].Update(gameTime);
            }

            // Dịch chỉ số bắt đầu cho frame tiếp theo
            _updateStartIndex = (_updateStartIndex + updatesThisFrame) % count;
        }

        /// <summary>Số component đang được quản lý.</summary>
        public int ComponentCount => _components.Count;
    }
}
