using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SandboxTuTien.Core.Combat;

namespace SandboxTuTien.Core
{
    /// <summary>Một đòn đánh gây sát thương lên Yêu Thú (để hiện số sát thương, rung màn, hạt).</summary>
    public class OnDamageDealtEvent
    {
        public Vector2 Position { get; set; }
        public Vector2 ImpactPosition { get; set; }
        public float Amount { get; set; }
        public Element Element { get; set; }
        public bool IsCounter { get; set; }
        public bool IsCrit { get; set; }

        /// <summary>Nguồn gây sát thương (Player hay Formation) — dùng để nhận diện đòn của người chơi cho nội tại như Kiếm Ý.</summary>
        public ProjectileOwner Owner { get; set; }
    }

    /// <summary>Một hiệu ứng trạng thái vừa được áp lên mục tiêu.</summary>
    public class OnStatusAppliedEvent
    {
        public Vector2 Position { get; set; }
        public StatusType Status { get; set; }
        public float Duration { get; set; }
    }

    /// <summary>Phản ứng nguyên tố xảy ra (VD: Hỏa tịnh độc).</summary>
    public class OnElementalReactionEvent
    {
        public Vector2 Position { get; set; }
        public string ReactionName { get; set; } = string.Empty;
    }

    // ========================================================================
    // EVENT DATA CLASSES
    // Mỗi event là một POCO class chứa dữ liệu context của sự kiện.
    // ========================================================================

    /// <summary>Sự kiện đột phá bình cảnh thành công (vượt xung quan hoặc Thiên Kiếp).</summary>
    public class OnBreakthroughSuccessEvent
    {
        public string PlayerName { get; set; } = string.Empty;
        public int Level { get; set; }
        public int BreakthroughNumber { get; set; }
        public bool WasHeavenlyTribulation { get; set; }
    }

    /// <summary>Sự kiện đột phá thất bại — đạo cơ tổn hại, rớt tu vi.</summary>
    public class OnBreakthroughFailedEvent
    {
        public string PlayerName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public float SuccessRate { get; set; }
        public int LevelLost { get; set; }
    }

    /// <summary>Sự kiện nhân vật tử vong.</summary>
    public class OnPlayerDiedEvent
    {
        public string PlayerName { get; set; } = string.Empty;
        public string CauseOfDeath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Một đợt lôi kiếp giáng xuống. Game1 lắng nghe để sinh các tia sét có báo hiệu
    /// quanh người chơi; trúng sét thì gọi CultivationComponent.TakeTribulationDamage.
    /// </summary>
    public class OnLightningStrikeEvent
    {
        public string PlayerName { get; set; } = string.Empty;
        public int Wave { get; set; }
        public int TotalWaves { get; set; }
        public int StrikeCount { get; set; }
        public float Damage { get; set; }
    }

    /// <summary>Sự kiện thay đổi đại cảnh giới.</summary>
    public class OnRealmChangedEvent
    {
        public string PlayerName { get; set; } = string.Empty;
        public string OldRealm { get; set; } = string.Empty;
        public string NewRealm { get; set; } = string.Empty;
        public int Level { get; set; }
    }

    /// <summary>Sự kiện tăng tầng tu vi.</summary>
    public class OnLevelUpEvent
    {
        public string PlayerName { get; set; } = string.Empty;
        public int OldLevel { get; set; }
        public int NewLevel { get; set; }
    }

    /// <summary>Sự kiện chạm bình cảnh (cần đột phá mới tu luyện tiếp được).</summary>
    public class OnBottleneckReachedEvent
    {
        public string PlayerName { get; set; } = string.Empty;
        public int Level { get; set; }
        public string CurrentRealm { get; set; } = string.Empty;
        public bool IsHeavenlyTribulation { get; set; }
    }

    // ========================================================================
    // EVENT MANAGER (EventBus Pattern)
    // Loosely-coupled pub/sub system sử dụng generic type dispatch.
    // ========================================================================

    /// <summary>
    /// EventBus trung tâm cho toàn bộ game. Các hệ thống subscribe/publish
    /// sự kiện thông qua class này mà không cần biết nhau trực tiếp.
    /// </summary>
    public class EventManager
    {
        private readonly Dictionary<Type, Delegate> _eventHandlers = new();

        /// <summary>
        /// Đăng ký lắng nghe một loại sự kiện.
        /// </summary>
        /// <typeparam name="T">Kiểu event data class.</typeparam>
        /// <param name="handler">Hàm callback khi event được publish.</param>
        public void Subscribe<T>(Action<T> handler) where T : class
        {
            var eventType = typeof(T);

            if (_eventHandlers.TryGetValue(eventType, out var existingHandler))
            {
                _eventHandlers[eventType] = Delegate.Combine(existingHandler, handler);
            }
            else
            {
                _eventHandlers[eventType] = handler;
            }
        }

        /// <summary>
        /// Hủy đăng ký lắng nghe một loại sự kiện.
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            var eventType = typeof(T);

            if (_eventHandlers.TryGetValue(eventType, out var existingHandler))
            {
                var newHandler = Delegate.Remove(existingHandler, handler);

                if (newHandler == null)
                {
                    _eventHandlers.Remove(eventType);
                }
                else
                {
                    _eventHandlers[eventType] = newHandler;
                }
            }
        }

        /// <summary>
        /// Phát sự kiện đến tất cả các subscriber đã đăng ký.
        /// </summary>
        /// <typeparam name="T">Kiểu event data class.</typeparam>
        /// <param name="eventData">Dữ liệu sự kiện.</param>
        public void Publish<T>(T eventData) where T : class
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData),
                    "[EventManager] Không thể publish event null.");
            }

            var eventType = typeof(T);

            if (_eventHandlers.TryGetValue(eventType, out var handler))
            {
                (handler as Action<T>)?.Invoke(eventData);
            }
        }

        /// <summary>
        /// Xóa toàn bộ subscriber (dùng khi cleanup/reset).
        /// </summary>
        public void ClearAll()
        {
            _eventHandlers.Clear();
        }

        /// <summary>
        /// Kiểm tra số lượng event type đang được lắng nghe (debug).
        /// </summary>
        public int RegisteredEventCount => _eventHandlers.Count;
    }
}
