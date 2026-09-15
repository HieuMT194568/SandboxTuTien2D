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
        NotLearned,
        OnCooldown,
        NotEnoughSpiritPower,
        Incapacitated
    }

    /// <summary>
    /// Hệ thống thi triển chiêu thức: kiểm tra điều kiện (đã lĩnh ngộ, hồi chiêu, Linh Lực),
    /// quản lý thời gian hồi chiêu và phóng chiêu theo dữ liệu JSON.
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
        public float GetCooldownRemaining(string techniqueId)
        {
            return _cooldowns.TryGetValue(techniqueId, out float remaining) ? remaining : 0f;
        }

        /// <summary>Xóa toàn bộ hồi chiêu (khi tải game).</summary>
        public void ResetCooldowns() => _cooldowns.Clear();

        /// <summary>
        /// Thử thi triển chiêu từ origin hướng về target. Chỉ tiêu hao Linh Lực khi thành công.
        /// </summary>
        public CastResult TryCast(CultivationComponent caster, TechniqueData? technique, Vector2 origin, Vector2 target)
        {
            if (caster.CurrentState == CultivationState.Dead || caster.CurrentState == CultivationState.Breakthrough)
                return CastResult.Incapacitated;

            if (technique == null)
                return CastResult.NotLearned;

            if (GetCooldownRemaining(technique.Id) > 0f)
                return CastResult.OnCooldown;

            if (!caster.ConsumeSpiritPower(technique.SPCost))
                return CastResult.NotEnoughSpiritPower;

            Execute(technique, origin, target);

            if (technique.Cooldown > 0f)
            {
                _cooldowns[technique.Id] = technique.Cooldown;
            }

            Console.WriteLine($"[Pháp Thuật] ⚡ {caster.OwnerName} thi triển: {technique.Name} " +
                              $"(Sát thương: {technique.Damage}, Linh lực: {technique.SPCost})");
            return CastResult.Success;
        }

        /// <summary>
        /// Phóng chiêu theo pattern: "fan" (quạt đều), "barrage" (chuỗi liên tiếp lệch ngẫu nhiên), "nova" (tỏa tròn).
        /// </summary>
        private void Execute(TechniqueData technique, Vector2 origin, Vector2 target)
        {
            Element element = ElementExtensions.ParseElement(technique.Element);
            var effects = technique.OnHitEffects;

            Vector2 dir = target - origin;
            if (dir == Vector2.Zero) dir = new Vector2(1, 0);
            else dir.Normalize();

            float baseAngle = (float)Math.Atan2(dir.Y, dir.X);
            int count = Math.Max(1, technique.Count);

            switch (technique.Pattern)
            {
                case "nova":
                {
                    float step = (float)(Math.PI * 2 / count);
                    for (int i = 0; i < count; i++)
                    {
                        Spawn(origin, CombatMath.AngleToVector(step * i), technique, element, effects);
                    }
                    break;
                }

                case "barrage":
                {
                    for (int i = 0; i < count; i++)
                    {
                        float angle = baseAngle + (float)(_random.NextDouble() - 0.5) * technique.Spread;
                        Vector2 bulletDir = CombatMath.AngleToVector(angle);
                        Spawn(origin + bulletDir * (i * technique.Spacing), bulletDir, technique, element, effects);
                    }
                    break;
                }

                default: // "fan"
                {
                    if (count == 1)
                    {
                        Spawn(origin, dir, technique, element, effects);
                        break;
                    }

                    float step = technique.Spread / (count - 1);
                    float startAngle = baseAngle - technique.Spread / 2f;
                    for (int i = 0; i < count; i++)
                    {
                        Spawn(origin, CombatMath.AngleToVector(startAngle + step * i), technique, element, effects);
                    }
                    break;
                }
            }
        }

        private void Spawn(Vector2 position, Vector2 direction, TechniqueData technique, Element element, IReadOnlyList<OnHitEffect> effects)
        {
            _projectilePool.Spawn(position, direction, technique.Damage, technique.Range, technique.Speed, element,
                                  isSilent: false, onHitEffects: effects, owner: ProjectileOwner.Player);
        }
    }
}
