using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SandboxTuTien.Components;
using SandboxTuTien.Core.Combat;
using SandboxTuTien.Data.Models;

namespace SandboxTuTien.Systems
{
    /// <summary>Kết quả khi thử thi triển chiêu.</summary>
    public enum CastResult
    {
        Success,

        /// <summary>Ô chiêu không có chiêu nào được gán (lỗi dữ liệu).</summary>
        NotLearned,

        /// <summary>Chiêu tồn tại nhưng cảnh giới hiện tại chưa đủ để mở khóa.</summary>
        NotUnlocked,

        /// <summary>Kiểu ra đòn của chiêu chưa được thi triển hỗ trợ (chờ giai đoạn sau).</summary>
        NotSupported,

        OnCooldown,
        NotEnoughSpiritPower,
        Incapacitated
    }

    /// <summary>
    /// Dữ liệu đã tính sẵn (gồm hệ số thông thạo) để Game1 thi hành các kiểu chiêu không tự
    /// spawn projectile (Dash di chuyển nhân vật, Zone sinh vùng hiệu ứng).
    /// </summary>
    public readonly struct SkillCastOutcome
    {
        /// <summary>Sát thương một lần đã nhân hệ số thông thạo (Dash).</summary>
        public float EffectiveDamage { get; init; }

        /// <summary>Sát thương mỗi nhịp đã nhân hệ số thông thạo (Zone).</summary>
        public float EffectiveDamagePerTick { get; init; }
    }

    /// <summary>
    /// Hệ thống thi triển chiêu thức: kiểm tra mở khóa theo cảnh giới, hồi chiêu, Linh Lực,
    /// tính hệ số thông thạo (mastery) và ghi nhận lượt dùng. Tự thi triển kiểu "projectile"
    /// (spawn đạn); các kiểu "dash" và "zone" được xác nhận + tính toán ở đây nhưng Game1 mới
    /// là nơi thực sự di chuyển nhân vật / sinh vùng hiệu ứng (SkillSystem không giữ tham chiếu
    /// tới Player hay ZoneSystem để tránh phụ thuộc ngược).
    /// Các kiểu "melee_arc", "ground_aoe", "self_buff", "channel" trả về NotSupported — sẽ được
    /// thi triển ở giai đoạn tiếp theo khi các lưu phái khác Kiếm Tu được hoàn thiện.
    /// </summary>
    public class SkillSystem
    {
        private readonly ProjectilePool _projectilePool;
        private readonly Dictionary<string, float> _cooldowns = new();
        private readonly Random _random = new();

        public SkillSystem(ProjectilePool projectilePool)
        {
            _projectilePool = projectilePool ?? throw new ArgumentNullException(nameof(projectilePool));
        }

        /// <summary>Đếm lùi thời gian hồi chiêu.</summary>
        public void Update(float deltaTime)
        {
            if (_cooldowns.Count == 0) return;

            foreach (var id in new List<string>(_cooldowns.Keys))
            {
                float remaining = _cooldowns[id] - deltaTime;
                if (remaining <= 0f) _cooldowns.Remove(id);
                else _cooldowns[id] = remaining;
            }
        }

        /// <summary>Thời gian hồi chiêu còn lại (giây).</summary>
        public float GetCooldownRemaining(string skillId)
        {
            return _cooldowns.TryGetValue(skillId, out float remaining) ? remaining : 0f;
        }

        /// <summary>Xóa toàn bộ hồi chiêu (khi tải game).</summary>
        public void ResetCooldowns() => _cooldowns.Clear();

        /// <summary>
        /// Thử thi triển chiêu ở một ô trong bộ 6 ô của lưu phái. Chỉ tiêu hao Linh Lực và
        /// ghi nhận thông thạo khi thành công.
        /// </summary>
        public CastResult TryCast(CultivationComponent caster, SkillLoadoutComponent loadout, SkillSlot slot,
                                  Vector2 origin, Vector2 target, out SkillCastOutcome outcome)
        {
            outcome = default;

            if (caster.CurrentState == CultivationState.Dead || caster.CurrentState == CultivationState.Breakthrough)
                return CastResult.Incapacitated;

            var skill = loadout.GetSkill(slot);
            if (skill == null)
                return CastResult.NotLearned;

            if (!loadout.IsUnlocked(slot, caster.CurrentRealm))
                return CastResult.NotUnlocked;

            if (!SkillExecutionTypeExtensions.TryParse(skill.Type, out var execType))
                return CastResult.NotSupported;

            bool supported = execType switch
            {
                SkillExecutionType.Projectile => true,
                SkillExecutionType.Zone => true,
                SkillExecutionType.Dash => skill.DashDistance > 0f || skill.Teleport,
                _ => false // melee_arc / ground_aoe / self_buff / channel: chưa thi triển
            };
            if (!supported)
                return CastResult.NotSupported;

            if (GetCooldownRemaining(skill.Id) > 0f)
                return CastResult.OnCooldown;

            if (!caster.ConsumeSpiritPower(skill.SPCost))
                return CastResult.NotEnoughSpiritPower;

            float masteryMultiplier = loadout.GetMasteryMultiplier(skill);
            loadout.RecordUse(skill.Id);

            Element element = skill.ElementFromCaster ? caster.SpiritRootElement : ElementExtensions.ParseElement(skill.Element);

            if (execType == SkillExecutionType.Projectile)
            {
                ExecuteProjectile(skill, origin, target, element, masteryMultiplier);
            }

            if (skill.Cooldown > 0f)
            {
                _cooldowns[skill.Id] = skill.Cooldown;
            }

            outcome = new SkillCastOutcome
            {
                EffectiveDamage = skill.Damage * masteryMultiplier,
                EffectiveDamagePerTick = skill.DamagePerTick * masteryMultiplier
            };

            Console.WriteLine($"[Chiêu Thức] ⚡ {caster.OwnerName} thi triển: {skill.Name} " +
                              $"(Sát thương: {outcome.EffectiveDamage:F0}, Linh lực: {skill.SPCost})");
            return CastResult.Success;
        }

        /// <summary>
        /// Phóng chiêu theo pattern: "fan" (quạt đều), "barrage" (chuỗi liên tiếp lệch ngẫu nhiên), "nova" (tỏa tròn).
        /// </summary>
        private void ExecuteProjectile(SkillData skill, Vector2 origin, Vector2 target, Element element, float damageMultiplier)
        {
            var effects = skill.OnHitEffects;
            float damage = skill.Damage * damageMultiplier;

            Vector2 dir = target - origin;
            if (dir == Vector2.Zero) dir = new Vector2(1, 0);
            else dir.Normalize();

            float baseAngle = (float)Math.Atan2(dir.Y, dir.X);
            int count = Math.Max(1, skill.Count);

            switch (skill.Pattern)
            {
                case "nova":
                {
                    float step = (float)(Math.PI * 2 / count);
                    for (int i = 0; i < count; i++)
                    {
                        Spawn(origin, CombatMath.AngleToVector(step * i), skill, element, damage, effects);
                    }
                    break;
                }

                case "barrage":
                {
                    for (int i = 0; i < count; i++)
                    {
                        float angle = baseAngle + (float)(_random.NextDouble() - 0.5) * skill.Spread;
                        Vector2 bulletDir = CombatMath.AngleToVector(angle);
                        Spawn(origin + bulletDir * (i * skill.Spacing), bulletDir, skill, element, damage, effects);
                    }
                    break;
                }

                default: // "fan"
                {
                    if (count == 1)
                    {
                        Spawn(origin, dir, skill, element, damage, effects);
                        break;
                    }

                    float step = skill.Spread / (count - 1);
                    float startAngle = baseAngle - skill.Spread / 2f;
                    for (int i = 0; i < count; i++)
                    {
                        Spawn(origin, CombatMath.AngleToVector(startAngle + step * i), skill, element, damage, effects);
                    }
                    break;
                }
            }
        }

        private void Spawn(Vector2 position, Vector2 direction, SkillData skill, Element element, float damage,
                           IReadOnlyList<OnHitEffect> effects)
        {
            _projectilePool.Spawn(position, direction, damage, skill.Range, skill.Speed, element,
                                  isSilent: false, onHitEffects: effects, owner: ProjectileOwner.Player);
        }
    }
}
