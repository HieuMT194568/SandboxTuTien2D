using SandboxTuTien.Components;
using SandboxTuTien.Core;
using SandboxTuTien.Core.Combat;

namespace SandboxTuTien.Entities
{
    /// <summary>
    /// Thực thể Player — chỉ chứa data, không có logic render.
    /// Theo Component-Based Architecture: Player là một "entity" rỗng
    /// được gắn các Component (CultivationComponent, InventoryComponent, SkillLoadoutComponent, v.v.).
    /// </summary>
    public class Player
    {
        /// <summary>Đạo hiệu nhân vật.</summary>
        public string Name { get; set; }

        /// <summary>Component tu luyện — quản lý tu vi, cảnh giới, đột phá.</summary>
        public CultivationComponent Cultivation { get; set; }

        /// <summary>Túi trữ vật của người chơi.</summary>
        public InventoryComponent Inventory { get; set; }

        // ====================================================================
        // VỊ TRÍ (Position 2D thực tế cho di chuyển và ngắm bắn)
        // ====================================================================

        /// <summary>Tọa độ X trên bản đồ.</summary>
        public float PositionX { get; set; }

        /// <summary>Tọa độ Y trên bản đồ.</summary>
        public float PositionY { get; set; }

        /// <summary>Vị trí 2D dưới dạng Vector2.</summary>
        public Microsoft.Xna.Framework.Vector2 Position => new Microsoft.Xna.Framework.Vector2(PositionX, PositionY);

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        /// <summary>
        /// Tạo Player mới với CultivationComponent và InventoryComponent.
        /// SkillLoadoutComponent được Game1 gắn riêng sau khi biết lưu phái đã chọn.
        /// </summary>
        /// <param name="name">Đạo hiệu nhân vật.</param>
        /// <param name="eventManager">EventBus trung tâm.</param>
        /// <param name="innateLevel">Tầng tu vi ban đầu (0-10).</param>
        /// <param name="spiritRootMultiplier">Phẩm chất Linh Căn (0.0-2.0).</param>
        /// <param name="spiritRootElement">Hệ Linh Căn.</param>
        /// <param name="hpMultiplier">Hệ số nhân HP tối đa theo lưu phái.</param>
        /// <param name="spiritPowerMultiplier">Hệ số nhân Linh Lực tối đa theo lưu phái.</param>
        /// <param name="moveSpeedMultiplier">Hệ số nhân tốc độ di chuyển theo lưu phái.</param>
        public Player(string name, EventManager eventManager, int innateLevel = 1, float spiritRootMultiplier = 1.0f,
                      Element spiritRootElement = Element.None, float hpMultiplier = 1f,
                      float spiritPowerMultiplier = 1f, float moveSpeedMultiplier = 1f)
        {
            Name = name;

            Cultivation = new CultivationComponent(eventManager, innateLevel, spiritRootMultiplier,
                                                   spiritRootElement, hpMultiplier, spiritPowerMultiplier,
                                                   moveSpeedMultiplier)
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
