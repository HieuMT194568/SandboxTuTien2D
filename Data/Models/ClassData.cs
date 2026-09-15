using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SandboxTuTien.Data.Models
{
    /// <summary>
    /// Một nội tại (passive) thuần mô tả — không mang số liệu thi triển,
    /// các trường Value/ValuePercentage trong <see cref="Effect"/> nếu có do CombatSystem/StatsComponent
    /// đọc trực tiếp ở giai đoạn nối dây gameplay.
    /// </summary>
    public class ClassPassiveData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>Hiệu ứng nội tại nếu biểu diễn được bằng một ItemEffect đơn giản (VD: +15% chí mạng).</summary>
        [JsonPropertyName("effect")]
        public ItemEffect? Effect { get; set; }
    }

    /// <summary>
    /// Hệ số nhân chỉ số gốc của lưu phái so với chỉ số nền tảng chung (1.0 = 100%).
    /// </summary>
    public class StatMultipliers
    {
        [JsonPropertyName("hp")]
        public float HP { get; set; } = 1f;

        [JsonPropertyName("spirit_power")]
        public float SpiritPower { get; set; } = 1f;

        [JsonPropertyName("speed")]
        public float Speed { get; set; } = 1f;
    }

    /// <summary>
    /// Bộ 6 ô kỹ năng của lưu phái, tham chiếu tới <see cref="SkillData.Id"/> trong skills.json.
    /// Với Pháp Tu, skill_1/2/3 và ultimate khác nhau theo hệ Linh Căn nên nằm trong <see cref="ByElement"/>
    /// thay vì các trường cố định.
    /// </summary>
    public class ClassSkillSlots
    {
        [JsonPropertyName("basic")]
        public string Basic { get; set; } = string.Empty;

        [JsonPropertyName("skill_1")]
        public string? Skill1 { get; set; }

        [JsonPropertyName("skill_2")]
        public string? Skill2 { get; set; }

        [JsonPropertyName("skill_3")]
        public string? Skill3 { get; set; }

        [JsonPropertyName("dash")]
        public string Dash { get; set; } = string.Empty;

        [JsonPropertyName("ultimate")]
        public string? Ultimate { get; set; }

        /// <summary>
        /// Bộ chiêu skill_1/2/3 + ultimate riêng theo hệ Linh Căn, khóa là "Fire"/"Wood"/"Ice".
        /// Chỉ Pháp Tu dùng trường này; các lưu phái khác để trống và dùng Skill1/2/3/Ultimate ở trên.
        /// </summary>
        [JsonPropertyName("by_element")]
        public Dictionary<string, ClassSkillSlots>? ByElement { get; set; }
    }

    /// <summary>
    /// POCO mô tả một lưu phái tu tiên. Ánh xạ trực tiếp từ Content/Data/classes.json.
    /// Chọn một lần khi bắt đầu game mới; quyết định chỉ số gốc, pháp khí dùng được, nội tại và bộ chiêu.
    /// </summary>
    public class ClassData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("stat_multipliers")]
        public StatMultipliers StatMultipliers { get; set; } = new();

        /// <summary>Loại pháp khí lưu phái này dùng được (VD: "SWORD"), khớp MagicWeaponData nếu mở rộng thêm trường type.</summary>
        [JsonPropertyName("allowed_weapon_types")]
        public List<string> AllowedWeaponTypes { get; set; } = new();

        [JsonPropertyName("passives")]
        public List<ClassPassiveData> Passives { get; set; } = new();

        [JsonPropertyName("skills")]
        public ClassSkillSlots Skills { get; set; } = new();

        /// <summary>
        /// Lưu phái có bộ chiêu chia theo hệ Linh Căn (hiện chỉ Pháp Tu).
        /// </summary>
        [JsonIgnore]
        public bool HasElementalSkills => Skills.ByElement is { Count: > 0 };

        /// <summary>
        /// Lấy bộ chiêu thực tế theo hệ Linh Căn đã chọn (nếu lưu phái chia theo hệ),
        /// ngược lại trả về bộ chiêu cố định của lưu phái.
        /// </summary>
        public ClassSkillSlots GetSkillsForElement(string element)
        {
            if (Skills.ByElement != null && Skills.ByElement.TryGetValue(element, out var slots))
            {
                return slots;
            }
            return Skills;
        }
    }
}
