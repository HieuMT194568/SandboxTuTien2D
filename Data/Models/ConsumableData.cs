using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SandboxTuTien.Data.Models
{
    /// <summary>
    /// POCO class biểu diễn dữ liệu Thực Phẩm Phụ Trợ (Consumable / Food).
    /// Ánh xạ trực tiếp từ cấu trúc JSON trong CONTEXT_TUTIEN.md mục 4.B.
    ///
    /// Ví dụ: "Khôi Phục Hương Tràng" — xúc xích hồi phục của Áo Tư Tạp,
    /// hồi 20% HP + 10% Hồn Lực, hết hạn sau 12 canh giờ.
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
        /// Nguồn gốc vật phẩm: SOUL_SKILL_CREATED (do Hồn Kỹ tạo ra),
        /// CRAFTED (chế tạo), LOOTED (nhặt được), v.v.
        /// </summary>
        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        /// <summary>Cảnh giới tối thiểu để sử dụng.</summary>
        [JsonPropertyName("tier_required")]
        public int TierRequired { get; set; }

        /// <summary>Danh sách hiệu ứng khi sử dụng.</summary>
        [JsonPropertyName("effects")]
        public List<ItemEffect> Effects { get; set; } = new();

        /// <summary>
        /// Thời gian hư hỏng tính bằng giây game.
        /// VD: 43200 = 12 canh giờ cho Hương Tràng.
        /// Sau thời gian này, vật phẩm sẽ biến mất khỏi inventory.
        /// </summary>
        [JsonPropertyName("spoilage_time")]
        public float SpoilageTime { get; set; }
    }
}
