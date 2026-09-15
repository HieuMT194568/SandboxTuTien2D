using System.Collections.Generic;
using System.Text.Json.Serialization;
using SandboxTuTien.Core.Combat;

namespace SandboxTuTien.Data.Models
{
    /// <summary>
    /// POCO class biểu diễn hiệu ứng (effect) của vật phẩm hoặc chiêu thức.
    /// Dùng chung cho Pháp Khí / Pháp Thuật (hiệu ứng khi trúng) và Đan Dược (heal/buff).
    /// Ánh xạ trực tiếp từ JSON "effects" array.
    /// </summary>
    public class ItemEffect
    {
        /// <summary>
        /// Loại hiệu ứng: APPLY_STATUS (hoặc APPLY_DEBUFF) khi trúng mục tiêu; HEAL_HP, HEAL_HP_OVER_TIME,
        /// RECOVER_SPIRIT_POWER, BREAKTHROUGH_BUFF (cộng % tỷ lệ đột phá), GAIN_CULTIVATION (cộng tu vi) khi dùng đan.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>Trạng thái gây ra: POISON, BURN, ROOT, SLOW, FREEZE, STUN, SHIELD, ATTACK_UP...</summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>Giá trị tuyệt đối (VD: 10 sát thương/giây cho độc, 0.3 = làm chậm 30%).</summary>
        [JsonPropertyName("value")]
        public float Value { get; set; }

        /// <summary>Giá trị theo phần trăm (VD: 20% heal HP, 2% HP tối đa mỗi giây cho độc).</summary>
        [JsonPropertyName("value_percentage")]
        public float ValuePercentage { get; set; }

        /// <summary>Thời gian hiệu lực tính bằng giây.</summary>
        [JsonPropertyName("duration")]
        public float Duration { get; set; }

        /// <summary>Xác suất kích hoạt 0-1 (bỏ trống = luôn kích hoạt).</summary>
        [JsonPropertyName("chance")]
        public float? Chance { get; set; }
    }

    public static class ItemEffectExtensions
    {
        /// <summary>Lọc các hiệu ứng áp trạng thái khi trúng mục tiêu và dựng sẵn OnHitEffect.</summary>
        public static IReadOnlyList<OnHitEffect> ToOnHitEffects(this IEnumerable<ItemEffect> effects)
        {
            var result = new List<OnHitEffect>();
            foreach (var effect in effects)
            {
                if (effect.Type != "APPLY_STATUS" && effect.Type != "APPLY_DEBUFF") continue;
                if (!StatusEffect.TryParse(effect.Status, out var status)) continue;

                result.Add(new OnHitEffect(status, effect.Duration, effect.Value, effect.ValuePercentage, effect.Chance ?? 1f));
            }
            return result;
        }
    }
}
