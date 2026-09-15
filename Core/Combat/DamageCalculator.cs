using System;
using Microsoft.Xna.Framework;

namespace SandboxTuTien.Core.Combat
{
    /// <summary>
    /// Công thức sát thương dùng chung: khắc hệ và chí mạng.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>Hệ số khắc hệ mặc định (+50%).</summary>
        public const float DEFAULT_COUNTER_MULTIPLIER = 1.5f;

        /// <summary>Hệ số chí mạng mặc định (+50%).</summary>
        public const float DEFAULT_CRIT_MULTIPLIER = 1.5f;

        /// <summary>
        /// Vòng tròn khắc chế: Hỏa > Mộc > Băng > Hỏa.
        /// </summary>
        public static bool IsCounter(Element attacker, Element defender)
        {
            if (attacker == Element.None || defender == Element.None) return false;

            return (attacker == Element.Fire && defender == Element.Wood) ||
                   (attacker == Element.Wood && defender == Element.Ice) ||
                   (attacker == Element.Ice && defender == Element.Fire);
        }

        /// <summary>
        /// Sát thương cuối = Gốc × (khắc hệ ? counterMultiplier : 1) × (chí mạng ? critMultiplier : 1).
        /// </summary>
        public static float Compute(float baseDamage, Element attackElement, Element defenderElement,
                                    float counterMultiplier, float critChance, float critMultiplier, Random random,
                                    out bool isCounter, out bool isCrit)
        {
            isCounter = IsCounter(attackElement, defenderElement);
            float damage = isCounter ? baseDamage * counterMultiplier : baseDamage;

            isCrit = critChance > 0f && random.NextDouble() < critChance;
            if (isCrit)
            {
                damage *= critMultiplier;
            }

            return damage;
        }
    }

    /// <summary>Tiện ích hình học cho chiến đấu.</summary>
    public static class CombatMath
    {
        /// <summary>Vector đơn vị theo góc (radian).</summary>
        public static Vector2 AngleToVector(float angle)
        {
            return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }
    }
}
