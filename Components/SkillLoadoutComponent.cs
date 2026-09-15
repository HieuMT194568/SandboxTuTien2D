using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SandboxTuTien.Core.Combat;
using SandboxTuTien.Data.Models;

namespace SandboxTuTien.Components
{
    /// <summary>
    /// Component quản lý bộ 6 ô chiêu của một lưu phái (đánh thường/chiêu 1-3/lướt/tuyệt kỹ),
    /// việc mở khóa theo cảnh giới, và độ thông thạo (số lần dùng) từng chiêu để lên bậc công pháp.
    ///
    /// Tách biệt khỏi CultivationComponent theo Component-Based Architecture: CultivationComponent
    /// chỉ lo tu vi/đột phá, còn SkillLoadoutComponent lo "nhân vật này dùng được chiêu gì".
    /// </summary>
    public class SkillLoadoutComponent
    {
        /// <summary>Lưu phái đã chọn khi tạo nhân vật (cố định suốt game).</summary>
        public ClassData PlayerClass { get; }

        /// <summary>Hệ Linh Căn đã chọn — chỉ có ý nghĩa với lưu phái chia chiêu theo hệ (Pháp Tu).</summary>
        public Element ChosenElement { get; }

        private readonly Dictionary<string, SkillData> _skillsById;

        /// <summary>Số lần đã dùng thành công từng chiêu (skill_id → số lần), phục vụ lên bậc công pháp.</summary>
        private readonly Dictionary<string, int> _masteryUses = new();

        public SkillLoadoutComponent(ClassData playerClass, Element chosenElement, IReadOnlyList<SkillData> allSkills)
        {
            PlayerClass = playerClass ?? throw new ArgumentNullException(nameof(playerClass));
            ChosenElement = chosenElement;
            _skillsById = (allSkills ?? throw new ArgumentNullException(nameof(allSkills))).ToDictionary(s => s.Id);
        }

        /// <summary>Bộ 6 ô chiêu đang áp dụng (đã tra theo hệ Linh Căn nếu lưu phái chia theo hệ).</summary>
        private ClassSkillSlots ActiveSlots => PlayerClass.GetSkillsForElement(ChosenElement.ToString());

        /// <summary>Lấy dữ liệu chiêu ở một ô cụ thể (null nếu ô đó không được lưu phái gán chiêu).</summary>
        public SkillData? GetSkill(SkillSlot slot)
        {
            var slots = ActiveSlots;
            string? id = slot switch
            {
                SkillSlot.Basic => slots.Basic,
                SkillSlot.Skill1 => slots.Skill1,
                SkillSlot.Skill2 => slots.Skill2,
                SkillSlot.Skill3 => slots.Skill3,
                SkillSlot.Dash => slots.Dash,
                SkillSlot.Ultimate => slots.Ultimate,
                _ => null
            };
            return !string.IsNullOrEmpty(id) && _skillsById.TryGetValue(id, out var skill) ? skill : null;
        }

        /// <summary>Chiêu ở ô này đã mở khóa chưa (cảnh giới hiện tại đã đạt unlock_realm của chiêu).</summary>
        public bool IsUnlocked(SkillSlot slot, CultivationRealm currentRealm)
        {
            var skill = GetSkill(slot);
            if (skill == null) return false;
            if (!Enum.TryParse<CultivationRealm>(skill.UnlockRealm, true, out var required)) return false;
            return currentRealm >= required;
        }

        /// <summary>Toàn bộ chiêu đã mở khóa ở cảnh giới hiện tại (dùng để thông báo lĩnh ngộ khi lên cảnh giới).</summary>
        public IEnumerable<SkillData> GetUnlockedSkills(CultivationRealm realm)
        {
            foreach (SkillSlot slot in Enum.GetValues<SkillSlot>())
            {
                if (IsUnlocked(slot, realm))
                {
                    yield return GetSkill(slot)!;
                }
            }
        }

        /// <summary>Số lần đã dùng thành công một chiêu.</summary>
        public int GetMasteryUses(string skillId)
        {
            return _masteryUses.TryGetValue(skillId, out int count) ? count : 0;
        }

        /// <summary>
        /// Bậc công pháp hiện tại của chiêu (0 = Nhập Môn, tối đa = MasteryUsesPerTier.Length = Viên Mãn).
        /// </summary>
        public int GetMasteryTier(SkillData skill)
        {
            int uses = GetMasteryUses(skill.Id);
            int tier = 0;
            foreach (int threshold in skill.MasteryUsesPerTier)
            {
                if (uses >= threshold) tier++;
            }
            return tier;
        }

        /// <summary>Hệ số nhân hiệu quả (sát thương/hồi máu...) do bậc công pháp hiện tại mang lại.</summary>
        public float GetMasteryMultiplier(SkillData skill)
        {
            return 1f + GetMasteryTier(skill) * skill.MasteryBonusPerTier;
        }

        /// <summary>Ghi nhận một lần dùng chiêu thành công (gọi sau khi SkillSystem xác nhận Cast thành công).</summary>
        public void RecordUse(string skillId)
        {
            _masteryUses[skillId] = GetMasteryUses(skillId) + 1;
        }

        /// <summary>Tên bậc công pháp tiếng Việt: 0=Nhập Môn, 1=Tiểu Thành, 2=Đại Thành, 3+=Viên Mãn.</summary>
        public static string GetMasteryTierName(int tier)
        {
            return tier switch
            {
                0 => "Nhập Môn",
                1 => "Tiểu Thành",
                2 => "Đại Thành",
                _ => "Viên Mãn"
            };
        }

        /// <summary>Tuần tự hóa độ thông thạo thành JSON (lưu vào MySQL).</summary>
        public string SerializeMastery()
        {
            return JsonSerializer.Serialize(_masteryUses);
        }

        /// <summary>Khôi phục độ thông thạo từ JSON (tải từ MySQL). Bỏ qua nếu chuỗi rỗng hoặc lỗi định dạng.</summary>
        public void LoadMastery(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                if (data == null) return;

                _masteryUses.Clear();
                foreach (var (skillId, uses) in data)
                {
                    _masteryUses[skillId] = uses;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[Chiêu Thức] LỖI khi khôi phục độ thông thạo: {ex.Message}");
            }
        }
    }
}
