using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SandboxTuTien.Data.Models
{
    /// <summary>
    /// Thông số chiến đấu của Pháp Khí (combat statistics).
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

        /// <summary>Số phi kiếm/đạn phóng ra mỗi lần sử dụng.</summary>
        [JsonPropertyName("projectile_count")]
        public int ProjectileCount { get; set; }

        /// <summary>True nếu đòn đánh không kinh động Yêu Thú (không gây Aggro).</summary>
        [JsonPropertyName("silent_attack")]
        public bool SilentAttack { get; set; }
    }

    /// <summary>
    /// POCO class biểu diễn dữ liệu Pháp Khí (Phi Kiếm, Kiếm Hạp...).
    /// Ánh xạ trực tiếp từ Content/Data/phap_khi.json.
    /// </summary>
    public class MagicWeaponData
    {
        [JsonPropertyName("item_id")]
        public string ItemId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Luôn là "MAGIC_WEAPON".</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>Hệ nguyên tố của đòn đánh: None, Fire, Wood, Ice.</summary>
        [JsonPropertyName("element")]
        public string Element { get; set; } = "None";

        /// <summary>Cảnh giới tối thiểu (chỉ số CultivationRealm) cần đạt để chế tạo.</summary>
        [JsonPropertyName("tier_required")]
        public int TierRequired { get; set; }

        /// <summary>Thông số chiến đấu.</summary>
        [JsonPropertyName("combat_stats")]
        public CombatStats? CombatStats { get; set; }

        /// <summary>Danh sách hiệu ứng khi trúng mục tiêu.</summary>
        [JsonPropertyName("effects")]
        public List<ItemEffect> Effects { get; set; } = new();

        /// <summary>Công thức luyện khí (danh sách nguyên liệu).</summary>
        [JsonPropertyName("crafting_recipe")]
        public List<CraftingIngredient> CraftingRecipe { get; set; } = new();

        private IReadOnlyList<SandboxTuTien.Core.Combat.OnHitEffect>? _onHitEffects;

        /// <summary>Hiệu ứng trạng thái khi trúng mục tiêu, dựng sẵn từ Effects.</summary>
        [JsonIgnore]
        public IReadOnlyList<SandboxTuTien.Core.Combat.OnHitEffect> OnHitEffects => _onHitEffects ??= Effects.ToOnHitEffects();
    }
}
