using System;

namespace SandboxTuTien.Core.Combat
{
    /// <summary>
    /// Các loại hiệu ứng trạng thái dùng chung cho Yêu Thú và người chơi.
    /// Tên trong JSON viết hoa có gạch dưới, VD: "ROOT", "ATTACK_UP".
    /// </summary>
    public enum StatusType
    {
        Burn,            // Thiêu đốt: sát thương theo thời gian
        Poison,          // Trúng độc: sát thương theo thời gian
        Slow,            // Làm chậm: giảm tốc độ theo Value (0.3 = 30%)
        Freeze,          // Đóng băng: không thể di chuyển và hành động
        Root,            // Trói chân: không thể di chuyển
        Stun,            // Choáng: không thể di chuyển và hành động
        Shield,          // Hộ thuẫn: hấp thụ sát thương bằng Value
        AttackUp,        // Tăng công theo Value
        DefenseUp,       // Tăng thủ theo Value
        SpeedUp,         // Tăng tốc theo Value
        DamageReduction  // Giảm sát thương nhận vào theo Value
    }

    /// <summary>
    /// Một hiệu ứng trạng thái đang tác động lên mục tiêu.
    /// </summary>
    public class StatusEffect
    {
        public StatusType Type { get; }

        /// <summary>Thời gian còn lại (giây).</summary>
        public float Remaining { get; set; }

        /// <summary>
        /// Độ mạnh. DoT: sát thương cố định mỗi giây. Slow/buff: tỷ lệ (0.3 = 30%). Shield: lượng khiên còn lại.
        /// </summary>
        public float Value { get; set; }

        /// <summary>DoT: % HP tối đa mất mỗi giây.</summary>
        public float ValuePercentage { get; set; }

        /// <summary>Bộ đếm nhịp gây sát thương mỗi giây.</summary>
        public float TickTimer { get; set; }

        public StatusEffect(StatusType type, float duration, float value = 0f, float valuePercentage = 0f)
        {
            Type = type;
            Remaining = duration;
            Value = value;
            ValuePercentage = valuePercentage;
        }

        /// <summary>Đọc tên trạng thái từ JSON ("ATTACK_UP" → AttackUp).</summary>
        public static bool TryParse(string? value, out StatusType type)
        {
            return Enum.TryParse(value?.Replace("_", string.Empty), true, out type);
        }

        /// <summary>Tên hiển thị tiếng Việt.</summary>
        public static string GetDisplayName(StatusType type)
        {
            return type switch
            {
                StatusType.Burn => "Thiêu đốt",
                StatusType.Poison => "Trúng độc",
                StatusType.Slow => "Làm chậm",
                StatusType.Freeze => "Đóng băng",
                StatusType.Root => "Trói chân",
                StatusType.Stun => "Choáng",
                StatusType.Shield => "Hộ thuẫn",
                StatusType.AttackUp => "Tăng công",
                StatusType.DefenseUp => "Tăng thủ",
                StatusType.SpeedUp => "Tăng tốc",
                StatusType.DamageReduction => "Giảm sát thương",
                _ => type.ToString()
            };
        }

        /// <summary>True với các trạng thái khống chế (hiện kèm thời gian khi áp dụng).</summary>
        public static bool IsCrowdControl(StatusType type)
        {
            return type is StatusType.Root or StatusType.Stun or StatusType.Freeze or StatusType.Slow;
        }
    }

    /// <summary>
    /// Mô tả hiệu ứng gắn vào đòn đánh (pháp thuật, pháp khí, trận pháp), dựng sẵn từ dữ liệu JSON.
    /// </summary>
    public readonly struct OnHitEffect
    {
        public StatusType Status { get; }
        public float Duration { get; }
        public float Value { get; }
        public float ValuePercentage { get; }

        /// <summary>Xác suất kích hoạt (0-1).</summary>
        public float Chance { get; }

        public OnHitEffect(StatusType status, float duration, float value = 0f, float valuePercentage = 0f, float chance = 1f)
        {
            Status = status;
            Duration = duration;
            Value = value;
            ValuePercentage = valuePercentage;
            Chance = chance;
        }

        /// <summary>Tạo hiệu ứng thực tế, thời gian nhân theo kháng/yếu điểm của mục tiêu.</summary>
        public StatusEffect CreateEffect(float durationMultiplier = 1f)
        {
            return new StatusEffect(Status, Duration * durationMultiplier, Value, ValuePercentage);
        }
    }
}
