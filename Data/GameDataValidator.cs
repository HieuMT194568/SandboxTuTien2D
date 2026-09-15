using System;
using System.Collections.Generic;
using SandboxTuTien.Components;
using SandboxTuTien.Core.Combat;
using SandboxTuTien.Data.Models;

namespace SandboxTuTien.Data
{
    /// <summary>
    /// Kiểm tra tính toàn vẹn tham chiếu giữa classes.json và skills.json khi nạp dữ liệu:
    /// mọi skill_id lưu phái tham chiếu phải tồn tại, đúng lưu phái, đúng ô, cảnh giới hợp lệ
    /// và kiểu ra đòn hợp lệ. Không ném ngoại lệ — chỉ trả về danh sách lỗi để log cảnh báo,
    /// tránh game crash vì một lỗi đánh máy trong JSON (giống triết lý Offline Fallback của DataLoader).
    /// </summary>
    public static class GameDataValidator
    {
        /// <summary>Kiểm tra toàn bộ classes.json + skills.json, trả về danh sách mô tả lỗi (rỗng nếu hợp lệ).</summary>
        public static List<string> Validate(IReadOnlyList<ClassData> classes, IReadOnlyList<SkillData> skills)
        {
            var errors = new List<string>();
            var skillById = new Dictionary<string, SkillData>();

            foreach (var skill in skills)
            {
                if (string.IsNullOrEmpty(skill.Id))
                {
                    errors.Add("Có chiêu thức không có 'id'.");
                    continue;
                }

                if (!skillById.TryAdd(skill.Id, skill))
                {
                    errors.Add($"Trùng id chiêu thức: '{skill.Id}'.");
                }

                if (!SkillExecutionTypeExtensions.TryParse(skill.Type, out _))
                {
                    errors.Add($"Chiêu '{skill.Id}': type '{skill.Type}' không hợp lệ.");
                }

                if (!Enum.TryParse<CultivationRealm>(skill.UnlockRealm, true, out _))
                {
                    errors.Add($"Chiêu '{skill.Id}': unlock_realm '{skill.UnlockRealm}' không hợp lệ.");
                }

                foreach (var effect in skill.Effects)
                {
                    bool isStatusEffect = effect.Type is "APPLY_STATUS" or "APPLY_DEBUFF";
                    if (isStatusEffect && !StatusEffect.TryParse(effect.Status, out _))
                    {
                        errors.Add($"Chiêu '{skill.Id}': status '{effect.Status}' không hợp lệ.");
                    }
                }
            }

            var classIds = new HashSet<string>();
            foreach (var cls in classes)
            {
                if (!classIds.Add(cls.Id))
                {
                    errors.Add($"Trùng id lưu phái: '{cls.Id}'.");
                }
            }

            foreach (var cls in classes)
            {
                CheckSlot(cls, "basic", cls.Skills.Basic, SkillSlot.Basic, skillById, errors);
                CheckSlot(cls, "dash", cls.Skills.Dash, SkillSlot.Dash, skillById, errors);

                if (cls.HasElementalSkills)
                {
                    foreach (var (element, slots) in cls.Skills.ByElement!)
                    {
                        CheckSlot(cls, $"skill_1 ({element})", slots.Skill1, SkillSlot.Skill1, skillById, errors);
                        CheckSlot(cls, $"skill_2 ({element})", slots.Skill2, SkillSlot.Skill2, skillById, errors);
                        CheckSlot(cls, $"skill_3 ({element})", slots.Skill3, SkillSlot.Skill3, skillById, errors);
                        CheckSlot(cls, $"ultimate ({element})", slots.Ultimate, SkillSlot.Ultimate, skillById, errors);
                    }
                }
                else
                {
                    CheckSlot(cls, "skill_1", cls.Skills.Skill1, SkillSlot.Skill1, skillById, errors);
                    CheckSlot(cls, "skill_2", cls.Skills.Skill2, SkillSlot.Skill2, skillById, errors);
                    CheckSlot(cls, "skill_3", cls.Skills.Skill3, SkillSlot.Skill3, skillById, errors);
                    CheckSlot(cls, "ultimate", cls.Skills.Ultimate, SkillSlot.Ultimate, skillById, errors);
                }
            }

            return errors;
        }

        /// <summary>Kiểm tra một ô kỹ năng: chiêu tồn tại, đúng lưu phái sở hữu, đúng ô đã khai báo.</summary>
        private static void CheckSlot(ClassData cls, string label, string? skillId, SkillSlot expectedSlot,
                                      Dictionary<string, SkillData> skillById, List<string> errors)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                errors.Add($"Lưu phái '{cls.Id}': ô '{label}' chưa gán chiêu thức.");
                return;
            }

            if (!skillById.TryGetValue(skillId, out var skill))
            {
                errors.Add($"Lưu phái '{cls.Id}': ô '{label}' tham chiếu chiêu '{skillId}' không tồn tại trong skills.json.");
                return;
            }

            if (skill.ClassId != cls.Id)
            {
                errors.Add($"Chiêu '{skillId}' gán cho lưu phái '{cls.Id}' nhưng class_id khai báo là '{skill.ClassId}'.");
            }

            if (!SkillSlotExtensions.TryParse(skill.Slot, out var actualSlot) || actualSlot != expectedSlot)
            {
                errors.Add($"Chiêu '{skillId}' đặt ở ô '{label}' nhưng slot khai báo trong skills.json là '{skill.Slot}'.");
            }
        }
    }
}
