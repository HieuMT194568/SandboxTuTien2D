using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SandboxTuTien.Core.Combat;
using SandboxTuTien.Data.Models;

namespace SandboxTuTien.Systems
{
    /// <summary>
    /// Một vùng hiệu ứng đang hoạt động (kiểu chiêu "zone"): đứng yên tại vị trí thi triển,
    /// gây sát thương hoặc hồi máu theo nhịp cho tới khi hết thời gian.
    /// </summary>
    public class ActiveZone
    {
        public Vector2 Position { get; init; }
        public float Radius { get; init; }
        public float Remaining { get; set; }
        public float TickInterval { get; init; }
        public float TickTimer { get; set; }
        public float DamagePerTick { get; init; }
        public float HealPercentPerTick { get; init; }
        public Element Element { get; init; }
        public IReadOnlyList<OnHitEffect> OnHitEffects { get; init; } = System.Array.Empty<OnHitEffect>();

        public bool Active => Remaining > 0f;
    }

    /// <summary>
    /// Hệ thống quản lý các ActiveZone: đếm thời gian, gây sát thương/hồi máu mỗi nhịp.
    /// Game1 tự vẽ vòng tròn hiệu ứng dựa trên <see cref="Zones"/>.
    /// </summary>
    public class ZoneSystem
    {
        private readonly List<ActiveZone> _zones = new();
        private readonly CombatSystem _combatSystem;

        public IReadOnlyList<ActiveZone> Zones => _zones;

        public ZoneSystem(CombatSystem combatSystem)
        {
            _combatSystem = combatSystem;
        }

        /// <summary>Sinh một vùng hiệu ứng mới từ dữ liệu chiêu, sát thương/tick đã áp mastery bonus.</summary>
        public void Spawn(Vector2 position, SkillData skill, float effectiveDamagePerTick)
        {
            if (skill.Radius <= 0f || skill.Duration <= 0f) return;

            _zones.Add(new ActiveZone
            {
                Position = position,
                Radius = skill.Radius,
                Remaining = skill.Duration,
                TickInterval = skill.TickInterval > 0f ? skill.TickInterval : 1f,
                DamagePerTick = effectiveDamagePerTick,
                HealPercentPerTick = skill.HealPercentPerTick,
                Element = ElementExtensions.ParseElement(skill.Element),
                OnHitEffects = skill.OnHitEffects
            });
        }

        /// <summary>Cập nhật mọi vùng: đếm thời gian, gây sát thương lên Yêu Thú, hồi máu người chơi nếu đứng trong vùng.</summary>
        public void Update(float deltaTime, Vector2 playerPosition, float playerMaxHp, out float playerHealAmount)
        {
            playerHealAmount = 0f;

            for (int i = _zones.Count - 1; i >= 0; i--)
            {
                var zone = _zones[i];
                zone.Remaining -= deltaTime;
                zone.TickTimer += deltaTime;

                while (zone.TickTimer >= zone.TickInterval)
                {
                    zone.TickTimer -= zone.TickInterval;

                    if (zone.DamagePerTick > 0f)
                    {
                        foreach (var monster in _combatSystem.QueryCircle(zone.Position, zone.Radius))
                        {
                            _combatSystem.ApplyHit(monster, zone.DamagePerTick, zone.Element, isSilent: false,
                                                   zone.OnHitEffects, ProjectileOwner.Player, monster.Position);
                        }
                    }

                    if (zone.HealPercentPerTick > 0f && Vector2.Distance(playerPosition, zone.Position) <= zone.Radius)
                    {
                        playerHealAmount += playerMaxHp * zone.HealPercentPerTick / 100f;
                    }
                }

                if (!zone.Active)
                {
                    _zones.RemoveAt(i);
                }
            }
        }

        /// <summary>Xóa toàn bộ vùng hiệu ứng (khi tải game/reset thế giới).</summary>
        public void Clear() => _zones.Clear();
    }
}
