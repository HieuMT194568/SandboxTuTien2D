using Microsoft.Xna.Framework;

namespace SandboxTuTien.Entities
{
    /// <summary>
    /// Vật phẩm trôi nổi trên mặt đất để người chơi đi qua nhặt.
    /// </summary>
    public class DroppedItem
    {
        public Vector2 Position { get; set; }
        public string ItemId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // CONSUMABLE hoặc HIDDEN_WEAPON
        public int Quantity { get; set; }
        public bool Active { get; set; }
        public float HoverTimer { get; set; }

        public DroppedItem(Vector2 position, string itemId, string name, string type, int quantity)
        {
            Position = position;
            ItemId = itemId;
            Name = name;
            Type = type;
            Quantity = quantity;
            Active = true;
            HoverTimer = 0f;
        }

        public void Update(float deltaTime)
        {
            HoverTimer += deltaTime;
        }
    }
}
