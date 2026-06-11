using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SandboxTuTien.Data.Models
{
    /// <summary>
    /// Thông số chiến đấu của Ám Khí (combat statistics).
    /// Ánh xạ từ JSON "combat_stats" object.
    /// </summary>
    public class CombatStats
    {
        /// <summary>Sát thương cơ bản mỗi lần bắn trúng.</summary>
        [JsonPropertyName("base_damage")]
        public float BaseDamage { get; set; }

        /// <summary>Tầm bắn tính bằng tiles/units.</summary>
        [JsonPropertyName("range")]
        public float Range { get; set; }

        /// <summary>Số projectile phóng ra mỗi lần sử dụng.</summary>
        [JsonPropertyName("projectile_count")]
        public int ProjectileCount { get; set; }

        /// <summary>True nếu đòn đánh không phát tiếng (VD: Vô Thanh Tụ Tiễn).</summary>
        [JsonPropertyName("silent_attack")]
        public bool SilentAttack { get; set; }
    }

    /// <summary>
    /// POCO class biểu diễn dữ liệu Ám Khí (Hidden Weapon).
    /// Ánh xạ trực tiếp từ cấu trúc JSON trong CONTEXT_TUTIEN.md mục 4.A.
    /// 
    /// Ví dụ: "Vô Thanh Tụ Tiễn" — ám khí phóng 3 mũi, gây poison,
    /// yêu cầu Thiết Mẫu + Lò Xo Cơ Quan để chế tạo.
    /// </summary>
    public class HiddenWeaponData
    {
        [JsonPropertyName("item_id")]
        public string ItemId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Luôn là "HIDDEN_WEAPON".</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>Cảnh giới tối thiểu (tier) cần đạt để sử dụng/chế tạo.</summary>
        [JsonPropertyName("tier_required")]
        public int TierRequired { get; set; }

        /// <summary>Thông số chiến đấu.</summary>
        [JsonPropertyName("combat_stats")]
        public CombatStats? CombatStats { get; set; }

        /// <summary>Danh sách hiệu ứng khi trúng mục tiêu.</summary>
        [JsonPropertyName("effects")]
        public List<ItemEffect> Effects { get; set; } = new();

        /// <summary>Công thức chế tác (danh sách nguyên liệu).</summary>
        [JsonPropertyName("crafting_recipe")]
        public List<CraftingIngredient> CraftingRecipe { get; set; } = new();
    }
}
