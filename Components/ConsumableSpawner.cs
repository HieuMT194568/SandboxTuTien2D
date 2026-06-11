using System;
using Microsoft.Xna.Framework;

namespace SandboxTuTien.Components
{
    /// <summary>
    /// Component sản sinh vật phẩm tiêu hao/đạn dược tự động theo chu kỳ thời gian.
    /// Có thể gắn vào NPC (Oscar) hoặc công trình (Lò Rèn).
    /// </summary>
    public class ConsumableSpawner
    {
        public Vector2 Position { get; set; }
        public float SpawnInterval { get; set; }
        public float Timer { get; set; }
        public string ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemType { get; set; } // CONSUMABLE hoặc HIDDEN_WEAPON
        public int Quantity { get; set; }

        /// <summary>Sự kiện kích hoạt khi sinh vật phẩm thành công.</summary>
        public event Action<Vector2, string, string, string, int>? OnSpawn;

        public ConsumableSpawner(Vector2 position, float spawnInterval, string itemId, string itemName, string itemType, int quantity)
        {
            Position = position;
            SpawnInterval = spawnInterval;
            Timer = 0f;
            ItemId = itemId;
            ItemName = itemName;
            ItemType = itemType;
            Quantity = quantity;
        }

        public void Update(float deltaTime)
        {
            Timer += deltaTime;
            if (Timer >= SpawnInterval)
            {
                Timer = 0f;
                OnSpawn?.Invoke(Position, ItemId, ItemName, ItemType, Quantity);
            }
        }
    }
}
