using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using SandboxTuTien.Core;
using SandboxTuTien.Core.Combat;
using SandboxTuTien.Entities;

namespace SandboxTuTien.Systems
{
    /// <summary>Kết quả của một đòn đánh trúng.</summary>
    public readonly struct HitResult
    {
        public float Damage { get; }
        public bool IsCounter { get; }
        public bool IsCrit { get; }
        public bool Killed { get; }

        public HitResult(float damage, bool isCounter, bool isCrit, bool killed)
        {
            Damage = damage;
            IsCounter = isCounter;
            IsCrit = isCrit;
            Killed = killed;
        }
    }

    /// <summary>
    /// Hệ thống chiến đấu: va chạm đạn, tìm mục tiêu theo vùng, tính sát thương,
    /// áp hiệu ứng trạng thái và phản ứng nguyên tố. Không vẽ gì — thông báo qua EventManager.
    /// </summary>
    public class CombatSystem
    {
        private readonly List<Monster> _monsters;
        private readonly ProjectilePool _projectilePool;
        private readonly EventManager _eventManager;
        private readonly Random _random = new();

        /// <summary>Hiệu ứng cộng thêm cho mọi đòn của người chơi (VD: Vạn Độc Thể 25% tẩm độc).</summary>
        public IReadOnlyList<OnHitEffect> PlayerBonusOnHit { get; set; } = Array.Empty<OnHitEffect>();

        /// <summary>Hệ số khắc hệ cho đòn của người chơi.</summary>
        public float PlayerCounterMultiplier { get; set; } = DamageCalculator.DEFAULT_COUNTER_MULTIPLIER;

        /// <summary>Tỷ lệ chí mạng của người chơi (0-1).</summary>
        public float PlayerCritChance { get; set; }

        /// <summary>Hệ số chí mạng của người chơi.</summary>
        public float PlayerCritMultiplier { get; set; } = DamageCalculator.DEFAULT_CRIT_MULTIPLIER;

        /// <summary>Hệ số nhân sát thương chung cho đòn của người chơi (VD: Kiếm Ý — cộng dồn theo combo).</summary>
        public float PlayerDamageMultiplier { get; set; } = 1f;

        /// <summary>Biên bản đồ dùng để giới hạn hiệu ứng đẩy lùi trong phạm vi bản đồ (Game1.MAP_WIDTH/HEIGHT dùng chung giá trị này).</summary>
        public const float MAP_BOUND = 2000f;

        public CombatSystem(List<Monster> monsters, ProjectilePool projectilePool, EventManager eventManager)
        {
            _monsters = monsters ?? throw new ArgumentNullException(nameof(monsters));
            _projectilePool = projectilePool ?? throw new ArgumentNullException(nameof(projectilePool));
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        }

        /// <summary>Xử lý va chạm giữa đạn đang bay và Yêu Thú.</summary>
        public void Update()
        {
            foreach (var proj in _projectilePool.Projectiles)
            {
                if (!proj.Active) continue;

                for (int i = 0; i < _monsters.Count; i++)
                {
                    var monster = _monsters[i];
                    if (!monster.Active || !monster.CheckCollision(proj.Position)) continue;

                    proj.Active = false;
                    ApplyHit(monster, proj.Damage, proj.Element, proj.IsSilent, proj.OnHitEffects, proj.Owner, proj.Position);
                    break; // Yêu Thú có thể đã bị xóa khỏi danh sách khi chết
                }
            }
        }

        /// <summary>Yêu Thú còn sống chạm vào hình tròn (trả về bản sao để an toàn khi mục tiêu chết).</summary>
        public List<Monster> QueryCircle(Vector2 center, float radius)
        {
            return _monsters.Where(m => m.Active && Vector2.Distance(m.Position, center) <= radius + m.Radius).ToList();
        }

        /// <summary>
        /// Yêu Thú còn sống nằm trong hình quạt từ origin theo direction, bán kính range, nửa góc halfAngle (radian).
        /// </summary>
        public List<Monster> QueryArc(Vector2 origin, Vector2 direction, float range, float halfAngle)
        {
            if (direction == Vector2.Zero) direction = new Vector2(1, 0);
            direction.Normalize();

            var result = new List<Monster>();
            foreach (var monster in _monsters)
            {
                if (!monster.Active) continue;

                Vector2 toMonster = monster.Position - origin;
                float distance = toMonster.Length();
                if (distance - monster.Radius > range) continue;

                if (distance < 1f)
                {
                    result.Add(monster);
                    continue;
                }

                float angle = (float)Math.Acos(Math.Clamp(Vector2.Dot(toMonster / distance, direction), -1f, 1f));
                float angularRadius = (float)Math.Atan(monster.Radius / distance);
                if (angle <= halfAngle + angularRadius)
                {
                    result.Add(monster);
                }
            }
            return result;
        }

        /// <summary>
        /// Áp một đòn đánh lên Yêu Thú: kinh động, phản ứng nguyên tố, tính sát thương, hiệu ứng trạng thái.
        /// </summary>
        public HitResult ApplyHit(Monster target, float baseDamage, Element element, bool isSilent,
                                  IReadOnlyList<OnHitEffect>? onHitEffects, ProjectileOwner owner, Vector2 impactPosition)
        {
            if (!isSilent)
            {
                target.IsAggroed = true;
            }

            // Phản ứng nguyên tố: Hỏa thiêu tịnh độc tố
            if (element == Element.Fire && target.StatusEffects.Has(StatusType.Poison))
            {
                target.StatusEffects.Remove(StatusType.Poison);
                Console.WriteLine($"[Hỏa Tịnh Độc] ★ Liệt hỏa thiêu rụi toàn bộ độc tố trên người {target.Name}!");
                _eventManager.Publish(new OnElementalReactionEvent { Position = target.Position, ReactionName = "Hỏa tịnh độc" });
            }

            bool fromPlayer = owner == ProjectileOwner.Player;
            float damage = DamageCalculator.Compute(
                baseDamage, element, target.Element,
                fromPlayer ? PlayerCounterMultiplier : DamageCalculator.DEFAULT_COUNTER_MULTIPLIER,
                fromPlayer ? PlayerCritChance : 0f,
                PlayerCritMultiplier, _random,
                out bool isCounter, out bool isCrit);

            if (fromPlayer) damage *= PlayerDamageMultiplier;

            if (onHitEffects != null) ApplyEffects(target, onHitEffects);
            if (fromPlayer) ApplyEffects(target, PlayerBonusOnHit);

            Vector2 targetPosition = target.Position;
            bool killed = target.TakeDamage(damage, element, isCounter);

            _eventManager.Publish(new OnDamageDealtEvent
            {
                Position = targetPosition,
                ImpactPosition = impactPosition,
                Amount = damage,
                Element = element,
                IsCounter = isCounter,
                IsCrit = isCrit,
                Owner = owner
            });

            return new HitResult(damage, isCounter, isCrit, killed);
        }

        /// <summary>Đẩy lùi mục tiêu theo hướng từ source ra xa, giới hạn trong biên bản đồ.</summary>
        public void ApplyKnockback(Monster target, Vector2 source, float force)
        {
            if (force <= 0f) return;

            Vector2 dir = target.Position - source;
            if (dir == Vector2.Zero) dir = new Vector2(1f, 0f);
            else dir.Normalize();

            Vector2 pushed = target.Position + dir * force;
            target.Position = new Vector2(
                Math.Clamp(pushed.X, 16f, MAP_BOUND - 16f),
                Math.Clamp(pushed.Y, 16f, MAP_BOUND - 16f));
        }

        /// <summary>Áp danh sách hiệu ứng lên mục tiêu (tung xúc xắc theo Chance).</summary>
        public void ApplyEffects(Monster target, IReadOnlyList<OnHitEffect> effects)
        {
            foreach (var effect in effects)
            {
                if (effect.Chance < 1f && _random.NextDouble() >= effect.Chance) continue;

                var status = effect.CreateEffect(target.GetStatusDurationMultiplier(effect.Status));
                target.StatusEffects.Apply(status);

                _eventManager.Publish(new OnStatusAppliedEvent
                {
                    Position = target.Position,
                    Status = effect.Status,
                    Duration = status.Remaining
                });
            }
        }
    }
}
