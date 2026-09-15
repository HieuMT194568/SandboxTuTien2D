using System.Text.Json.Serialization;

namespace SandboxTuTien.Data.Models
{
    /// <summary>
    /// POCO class biểu diễn hiệu ứng (effect) của vật phẩm.
    /// Dùng chung cho cả Pháp Khí (debuff) và Đan Dược (heal/buff).
    /// Ánh xạ trực tiếp từ JSON "effects" array.
    /// </summary>
    public class ItemEffect
    {
        /// <summary>
        /// Loại hiệu ứng: APPLY_DEBUFF, HEAL_HP, HEAL_HP_OVER_TIME, RECOVER_SPIRIT_POWER,
        /// BREAKTHROUGH_BUFF (cộng % tỷ lệ đột phá), GAIN_CULTIVATION (cộng tu vi).
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>Trạng thái gây ra (nếu là debuff): POISON, BLEED, STUN...</summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>Giá trị tuyệt đối (VD: 10 damage/tick cho poison).</summary>
        [JsonPropertyName("value")]
        public float Value { get; set; }

        /// <summary>Giá trị theo phần trăm (VD: 20% heal HP).</summary>
        [JsonPropertyName("value_percentage")]
        public float ValuePercentage { get; set; }

        /// <summary>Thời gian hiệu lực tính bằng giây game.</summary>
        [JsonPropertyName("duration")]
        public float Duration { get; set; }
    }
}
