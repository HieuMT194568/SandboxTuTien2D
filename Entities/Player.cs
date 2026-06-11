using SandboxTuTien.Components;
using SandboxTuTien.Core;

namespace SandboxTuTien.Entities
{
    /// <summary>
    /// Thực thể Player — chỉ chứa data, không có logic render.
    /// Theo Component-Based Architecture: Player là một "entity" rỗng
    /// được gắn các Component (CultivationComponent, InventoryComponent, v.v.).
    ///
    /// Placeholder cho vị trí, inventory sẽ được mở rộng sau.
    /// </summary>
    public class Player
    {
        /// <summary>Tên nhân vật.</summary>
        public string Name { get; set; }

        /// <summary>Component tu luyện — quản lý Hồn Lực, cảnh giới, đột phá.</summary>
        public CultivationComponent Cultivation { get; set; }

        /// <summary>Túi đồ của người chơi.</summary>
        public InventoryComponent Inventory { get; set; }

        // ====================================================================
        // VỊ TRÍ (Position 2D thực tế cho di chuyển và ngắm bắn)
        // ====================================================================

        /// <summary>Tọa độ X trên bản đồ.</summary>
        public float PositionX { get; set; }

        /// <summary>Tọa độ Y trên bản đồ.</summary>
        public float PositionY { get; set; }

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        /// <summary>
        /// Tạo Player mới với CultivationComponent và InventoryComponent.
        /// </summary>
        /// <param name="name">Tên nhân vật.</param>
        /// <param name="eventManager">EventBus trung tâm.</param>
        /// <param name="innateLevel">Cấp Tiên Thiên Hồn Lực (0-10).</param>
        /// <param name="innateMultiplier">Hệ số tu luyện (0.0-2.0).</param>
        public Player(string name, EventManager eventManager,
                      int innateLevel = 1, float innateMultiplier = 1.0f)
        {
            Name = name;

            Cultivation = new CultivationComponent(eventManager, innateLevel, innateMultiplier)
            {
                OwnerName = name
            };

            Inventory = new InventoryComponent(eventManager);

            // Vị trí mặc định ở giữa màn hình
            PositionX = 400;
            PositionY = 240;
        }
    }
}
