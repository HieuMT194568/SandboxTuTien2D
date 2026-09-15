using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SandboxTuTien.Data.Models
{
    /// <summary>
    /// POCO class biểu diễn Đan Dược / Vật Liệu tiêu hao (Consumable).
    /// Ánh xạ trực tiếp từ Content/Data/consumables.json.
    ///
    /// Ví dụ: "Hồi Xuân Đan" — hồi 20% HP ngay và 5% HP/giây trong 10 giây.
    /// </summary>
    public class ConsumableData
    {
        [JsonPropertyName("item_id")]
        public string ItemId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Luôn là "CONSUMABLE".</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Nguồn gốc vật phẩm: NPC_ALCHEMIST (Đan Sư luyện), CRAFTED (tự luyện),
        /// MONSTER_DROPPED (Yêu Thú rơi), v.v.
        /// </summary>
        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        /// <summary>Cảnh giới tối thiểu để sử dụng.</summary>
        [JsonPropertyName("tier_required")]
        public int TierRequired { get; set; }

        /// <summary>Danh sách hiệu ứng khi sử dụng.</summary>
        [JsonPropertyName("effects")]
        public List<ItemEffect> Effects { get; set; } = new();

        /// <summary>Công thức luyện đan (rỗng nếu không luyện được).</summary>
        [JsonPropertyName("crafting_recipe")]
        public List<CraftingIngredient> CraftingRecipe { get; set; } = new();

        /// <summary>
        /// Thời gian đan dược mất dược lực tính bằng giây game (-1 = vĩnh viễn).
        /// Sau thời gian này, vật phẩm sẽ biến mất khỏi inventory.
        /// </summary>
        [JsonPropertyName("spoilage_time")]
        public float SpoilageTime { get; set; }
    }
}
