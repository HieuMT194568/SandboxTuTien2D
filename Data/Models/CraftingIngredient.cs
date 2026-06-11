using System.Text.Json.Serialization;

namespace SandboxTuTien.Data.Models
{
    /// <summary>
    /// POCO class biểu diễn một nguyên liệu trong công thức chế tác.
    /// Ánh xạ trực tiếp từ JSON "crafting_recipe" array.
    /// </summary>
    public class CraftingIngredient
    {
        /// <summary>ID của nguyên liệu (VD: "mat_thi_thiet_mau").</summary>
        [JsonPropertyName("item_id")]
        public string ItemId { get; set; } = string.Empty;

        /// <summary>Số lượng cần thiết.</summary>
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }
}
