using System;
using System.Collections.Generic;
using System.Linq;
using SandboxTuTien.Core.Combat;

namespace SandboxTuTien.Components
{
    /// <summary>
    /// Component quản lý hiệu ứng trạng thái (khống chế, sát thương theo thời gian, buff, khiên)
    /// của một thực thể. Mỗi loại trạng thái chỉ tồn tại một bản: áp dụng lại sẽ làm mới thời gian
    /// và giữ độ mạnh lớn nhất.
    /// </summary>
    public class StatusEffectComponent
    {
        private readonly List<StatusEffect> _effects = new();

        /// <summary>Danh sách trạng thái đang có.</summary>
        public IReadOnlyList<StatusEffect> Effects => _effects;

        /// <summary>Có thể di chuyển (không bị trói, choáng, đóng băng).</summary>
        public bool CanMove => !Has(StatusType.Root) && !Has(StatusType.Stun) && !Has(StatusType.Freeze);

        /// <summary>Có thể hành động/tấn công (không bị choáng, đóng băng).</summary>
        public bool CanAct => !Has(StatusType.Stun) && !Has(StatusType.Freeze);

        /// <summary>Hệ số tốc độ từ Làm chậm và Tăng tốc (tối thiểu 10%).</summary>
        public float SpeedMultiplier => Math.Max(0.1f, (1f - GetValue(StatusType.Slow)) * (1f + GetValue(StatusType.SpeedUp)));

        /// <summary>Áp dụng trạng thái mới hoặc làm mới trạng thái cùng loại.</summary>
        public void Apply(StatusEffect effect)
        {
            if (effect.Remaining <= 0f) return;

            var existing = _effects.FirstOrDefault(e => e.Type == effect.Type);
            if (existing == null)
            {
                _effects.Add(effect);
                return;
            }

            existing.Remaining = Math.Max(existing.Remaining, effect.Remaining);
            existing.Value = Math.Max(existing.Value, effect.Value);
            existing.ValuePercentage = Math.Max(existing.ValuePercentage, effect.ValuePercentage);
        }

        public bool Has(StatusType type) => _effects.Any(e => e.Type == type);

        /// <summary>Độ mạnh của trạng thái (0 nếu không có).</summary>
        public float GetValue(StatusType type) => _effects.FirstOrDefault(e => e.Type == type)?.Value ?? 0f;

        public void Remove(StatusType type) => _effects.RemoveAll(e => e.Type == type);

        public void Clear() => _effects.Clear();

        /// <summary>
        /// Khiên hấp thụ sát thương. Trả về phần sát thương còn lại sau khi trừ khiên.
        /// </summary>
        public float AbsorbWithShield(float damage)
        {
            var shield = _effects.FirstOrDefault(e => e.Type == StatusType.Shield);
            if (shield == null || damage <= 0f) return damage;

            float absorbed = Math.Min(shield.Value, damage);
            shield.Value -= absorbed;
            if (shield.Value <= 0f)
            {
                _effects.Remove(shield);
            }
            return damage - absorbed;
        }

        /// <summary>
        /// Đếm thời gian các trạng thái. Trả về tổng sát thương theo thời gian (Thiêu đốt, Độc)
        /// phát sinh trong frame này, tính mỗi giây một nhịp: Value + MaxHP × ValuePercentage%.
        /// </summary>
        public float Update(float deltaTime, float maxHp)
        {
            float damageOverTime = 0f;

            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                var effect = _effects[i];
                effect.Remaining -= deltaTime;

                if (effect.Type is StatusType.Burn or StatusType.Poison)
                {
                    effect.TickTimer += deltaTime;
                    while (effect.TickTimer >= 1f)
                    {
                        effect.TickTimer -= 1f;
                        damageOverTime += effect.Value + maxHp * effect.ValuePercentage / 100f;
                    }
                }

                if (effect.Remaining <= 0f)
                {
                    _effects.RemoveAt(i);
                }
            }

            return damageOverTime;
        }
    }
}
