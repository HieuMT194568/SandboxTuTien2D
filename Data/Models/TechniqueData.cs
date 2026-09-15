using System.Text.Json.Serialization;

namespace SandboxTuTien.Data.Models
{
    /// <summary>
    /// POCO class biểu diễn một Pháp Thuật chủ động (Q/E).
    /// Mỗi Linh Căn (hệ nguyên tố) mở khóa một pháp thuật ở mỗi lần đột phá (tier 1, 2).
    /// Ánh xạ trực tiếp từ Content/Data/techniques.json.
    /// </summary>
    public class TechniqueData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Hệ nguyên tố: None, Fire, Wood, Ice.</summary>
        [JsonPropertyName("element")]
        public string Element { get; set; } = "None";

        /// <summary>Thứ tự mở khóa: 1 = sau lần đột phá đầu tiên, 2 = sau lần thứ hai.</summary>
        [JsonPropertyName("tier")]
        public int Tier { get; set; }

        /// <summary>Linh Lực tiêu hao.</summary>
        [JsonPropertyName("sp_cost")]
        public float SPCost { get; set; }

        /// <summary>Sát thương mỗi tia.</summary>
        [JsonPropertyName("damage")]
        public float Damage { get; set; }

        /// <summary>
        /// Kiểu phóng: "fan" (quạt đều theo spread), "barrage" (chuỗi liên tiếp, lệch ngẫu nhiên spread),
        /// "nova" (tỏa tròn 360 độ).
        /// </summary>
        [JsonPropertyName("pattern")]
        public string Pattern { get; set; } = "fan";

        /// <summary>Số tia phóng ra.</summary>
        [JsonPropertyName("count")]
        public int Count { get; set; } = 1;

        /// <summary>Độ mở quạt (radian) cho "fan", hoặc biên độ lệch ngẫu nhiên cho "barrage".</summary>
        [JsonPropertyName("spread")]
        public float Spread { get; set; }

        /// <summary>Khoảng cách xếp hàng giữa các tia (px) cho "barrage".</summary>
        [JsonPropertyName("spacing")]
        public float Spacing { get; set; }

        [JsonPropertyName("range")]
        public float Range { get; set; }

        [JsonPropertyName("speed")]
        public float Speed { get; set; }
    }
}
