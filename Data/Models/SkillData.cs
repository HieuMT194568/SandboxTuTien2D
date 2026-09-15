using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SandboxTuTien.Data.Models
{
    /// <summary>
    /// Cách một chiêu thức ra đòn. Việc thi triển (execution) cho các kiểu ngoài
    /// "projectile" được xây dựng dần ở các giai đoạn sau — SkillData ở đây chỉ mô tả dữ liệu.
    /// </summary>
    public enum SkillExecutionType
    {
        /// <summary>Bắn đạn theo pattern (fan/barrage/nova) — đã có SkillSystem thi triển.</summary>
        Projectile,

        /// <summary>Cận chiến hình quạt trước mặt (CombatSystem.QueryArc).</summary>
        MeleeArc,

        /// <summary>Đòn xuống đất có báo hiệu (Delay giây) rồi mới gây sát thương vùng tròn.</summary>
        GroundAoe,

        /// <summary>Vùng hiệu ứng tồn tại lâu, gây sát thương/hồi máu mỗi nhịp.</summary>
        Zone,

        /// <summary>Buff/khiên lên chính mình trong một khoảng thời gian.</summary>
        SelfBuff,

        /// <summary>Lướt/dịch chuyển theo hướng di chuyển hoặc con trỏ.</summary>
        Dash,

        /// <summary>Vận công liên tục, gây sát thương mỗi nhịp trong lúc giữ chiêu.</summary>
        Channel
    }

    /// <summary>
    /// Vị trí của chiêu trong bộ 6 ô kỹ năng của một lưu phái.
    /// </summary>
    public enum SkillSlot
    {
        Basic,
        Skill1,
        Skill2,
        Skill3,
        Dash,
        Ultimate
    }

    public static class SkillExecutionTypeExtensions
    {
        /// <summary>Đọc kiểu ra đòn từ chuỗi JSON ("melee_arc" → MeleeArc, "ground_aoe" → GroundAoe...).</summary>
        public static bool TryParse(string? value, out SkillExecutionType type)
        {
            return Enum.TryParse(value?.Replace("_", string.Empty), true, out type);
        }
    }

    public static class SkillSlotExtensions
    {
        /// <summary>Đọc ô kỹ năng từ chuỗi JSON ("skill_1" → Skill1, "basic" → Basic...).</summary>
        public static bool TryParse(string? value, out SkillSlot slot)
        {
            return Enum.TryParse(value?.Replace("_", string.Empty), true, out slot);
        }
    }

    /// <summary>
    /// POCO mô tả một chiêu thức, dùng chung cho mọi lưu phái và mọi kiểu ra đòn.
    /// Ánh xạ trực tiếp từ Content/Data/skills.json.
    ///
    /// Tùy theo <see cref="Type"/>, chỉ một tập con các trường bên dưới có ý nghĩa —
    /// xem ghi chú trên từng trường để biết trường đó phục vụ kiểu nào.
    /// </summary>
    public class SkillData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Lưu phái sở hữu chiêu này (id trong classes.json).</summary>
        [JsonPropertyName("class_id")]
        public string ClassId { get; set; } = string.Empty;

        /// <summary>Ô kỹ năng: "basic", "skill_1", "skill_2", "skill_3", "dash", "ultimate".</summary>
        [JsonPropertyName("slot")]
        public string Slot { get; set; } = "skill_1";

        /// <summary>Kiểu ra đòn: "projectile", "melee_arc", "ground_aoe", "zone", "self_buff", "dash", "channel".</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "projectile";

        /// <summary>Hệ nguyên tố cố định của chiêu: None, Fire, Wood, Ice.</summary>
        [JsonPropertyName("element")]
        public string Element { get; set; } = "None";

        /// <summary>
        /// True nếu hệ của chiêu lấy theo Linh Căn của người thi triển thay vì cố định
        /// (VD: đòn đánh thường Linh Đạn của Pháp Tu).
        /// </summary>
        [JsonPropertyName("element_from_caster")]
        public bool ElementFromCaster { get; set; }

        /// <summary>
        /// Cảnh giới tối thiểu để mở khóa chiêu: "LuyenKhi", "TrucCo", "KimDan", "NguyenAnh"...
        /// (trùng tên với enum CultivationRealm).
        /// </summary>
        [JsonPropertyName("unlock_realm")]
        public string UnlockRealm { get; set; } = "LuyenKhi";

        [JsonPropertyName("sp_cost")]
        public float SPCost { get; set; }

        /// <summary>Thời gian hồi chiêu (giây).</summary>
        [JsonPropertyName("cooldown")]
        public float Cooldown { get; set; }

        /// <summary>Sát thương gốc mỗi lần trúng (Projectile/MeleeArc/GroundAoe/Channel-mỗi-nhịp).</summary>
        [JsonPropertyName("damage")]
        public float Damage { get; set; }

        // ================================================================
        // Projectile
        // ================================================================

        /// <summary>Projectile: "fan" (quạt đều), "barrage" (chuỗi lệch ngẫu nhiên), "nova" (tỏa tròn).</summary>
        [JsonPropertyName("pattern")]
        public string Pattern { get; set; } = "fan";

        [JsonPropertyName("count")]
        public int Count { get; set; } = 1;

        /// <summary>Projectile: độ mở quạt (radian) cho "fan", biên độ lệch cho "barrage".</summary>
        [JsonPropertyName("spread")]
        public float Spread { get; set; }

        /// <summary>Projectile "barrage": khoảng cách xếp hàng giữa các tia (px).</summary>
        [JsonPropertyName("spacing")]
        public float Spacing { get; set; }

        [JsonPropertyName("range")]
        public float Range { get; set; }

        [JsonPropertyName("speed")]
        public float Speed { get; set; }

        /// <summary>Projectile: xuyên qua mục tiêu thay vì biến mất khi trúng.</summary>
        [JsonPropertyName("pierce")]
        public bool Pierce { get; set; }

        // ================================================================
        // MeleeArc / GroundAoe / Zone: vùng ảnh hưởng
        // ================================================================

        /// <summary>Bán kính vùng ảnh hưởng (MeleeArc/GroundAoe/Zone), đơn vị px.</summary>
        [JsonPropertyName("radius")]
        public float Radius { get; set; }

        /// <summary>MeleeArc: độ mở góc quạt trước mặt, tính bằng độ (VD: 100 = ±50°).</summary>
        [JsonPropertyName("arc_angle_deg")]
        public float ArcAngleDeg { get; set; } = 100f;

        /// <summary>MeleeArc/GroundAoe/Dash: lực đẩy lùi mục tiêu (px).</summary>
        [JsonPropertyName("knockback")]
        public float Knockback { get; set; }

        /// <summary>GroundAoe: thời gian báo hiệu trước khi gây sát thương (giây) — tái dùng cơ chế Thiên Kiếp.</summary>
        [JsonPropertyName("delay")]
        public float Delay { get; set; }

        // ================================================================
        // Zone / SelfBuff / Channel: kéo dài theo thời gian
        // ================================================================

        /// <summary>Zone/SelfBuff/Channel/Dash(dạng tăng tốc): thời gian tồn tại (giây).</summary>
        [JsonPropertyName("duration")]
        public float Duration { get; set; }

        /// <summary>Zone/Channel: khoảng cách giữa mỗi nhịp gây sát thương/hồi máu (giây).</summary>
        [JsonPropertyName("tick_interval")]
        public float TickInterval { get; set; } = 1f;

        /// <summary>Zone/Channel: sát thương mỗi nhịp.</summary>
        [JsonPropertyName("damage_per_tick")]
        public float DamagePerTick { get; set; }

        /// <summary>Zone: % HP tối đa hồi cho đồng minh mỗi nhịp (VD: Thanh Mộc Hồi Xuân).</summary>
        [JsonPropertyName("heal_percent_per_tick")]
        public float HealPercentPerTick { get; set; }

        // ================================================================
        // SelfBuff
        // ================================================================

        /// <summary>SelfBuff: khiên bằng % HP tối đa của người thi triển.</summary>
        [JsonPropertyName("shield_percent_maxhp")]
        public float ShieldPercentMaxHp { get; set; }

        /// <summary>SelfBuff: % sát thương nhận vào được phản lại kẻ tấn công.</summary>
        [JsonPropertyName("damage_reflect_percent")]
        public float DamageReflectPercent { get; set; }

        /// <summary>SelfBuff: % công tăng thêm trong lúc buff còn hiệu lực.</summary>
        [JsonPropertyName("attack_bonus_percent")]
        public float AttackBonusPercent { get; set; }

        /// <summary>SelfBuff: % sát thương nhận vào được giảm trong lúc buff còn hiệu lực.</summary>
        [JsonPropertyName("damage_reduction_percent")]
        public float DamageReductionPercent { get; set; }

        /// <summary>SelfBuff (Phù Trận Sư): % tăng sức mạnh Trận Pháp gần đó.</summary>
        [JsonPropertyName("formation_power_bonus_percent")]
        public float FormationPowerBonusPercent { get; set; }

        // ================================================================
        // Dash
        // ================================================================

        /// <summary>Dash: quãng đường lướt (px).</summary>
        [JsonPropertyName("dash_distance")]
        public float DashDistance { get; set; }

        /// <summary>Dash: tốc độ lướt (px/giây).</summary>
        [JsonPropertyName("dash_speed")]
        public float DashSpeed { get; set; }

        /// <summary>Dash: dịch chuyển tức thời thay vì trượt (VD: Súc Địa Thành Thốn).</summary>
        [JsonPropertyName("teleport")]
        public bool Teleport { get; set; }

        /// <summary>Dash dạng tăng tốc (VD: Thần Hành Phù): % tốc độ di chuyển tăng thêm trong Duration giây.</summary>
        [JsonPropertyName("speed_bonus_percent")]
        public float SpeedBonusPercent { get; set; }

        // ================================================================
        // Chung
        // ================================================================

        /// <summary>Hiệu ứng trạng thái áp lên mục tiêu khi trúng.</summary>
        [JsonPropertyName("effects")]
        public List<ItemEffect> Effects { get; set; } = new();

        /// <summary>
        /// Số lần dùng cần để lên mỗi bậc công pháp (Nhập Môn → Tiểu Thành → Đại Thành → Viên Mãn),
        /// 3 mốc cho 3 lần thăng bậc.
        /// </summary>
        [JsonPropertyName("mastery_uses_per_tier")]
        public int[] MasteryUsesPerTier { get; set; } = { 50, 150, 400 };

        /// <summary>% sát thương/hiệu quả tăng thêm mỗi bậc công pháp đạt được.</summary>
        [JsonPropertyName("mastery_bonus_per_tier")]
        public float MasteryBonusPerTier { get; set; } = 0.05f;

        private IReadOnlyList<SandboxTuTien.Core.Combat.OnHitEffect>? _onHitEffects;

        /// <summary>Hiệu ứng trạng thái khi trúng, dựng sẵn từ Effects.</summary>
        [JsonIgnore]
        public IReadOnlyList<SandboxTuTien.Core.Combat.OnHitEffect> OnHitEffects => _onHitEffects ??= Effects.ToOnHitEffects();
    }
}
