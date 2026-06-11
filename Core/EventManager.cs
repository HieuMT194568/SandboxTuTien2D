using System;
using System.Collections.Generic;

namespace SandboxTuTien.Core
{
    // ========================================================================
    // EVENT DATA CLASSES
    // Mỗi event là một POCO class chứa dữ liệu context của sự kiện.
    // ========================================================================

    /// <summary>Sự kiện đột phá thành công — hấp thu Hồn Hoàn thành công.</summary>
    public class OnBreakthroughSuccessEvent
    {
        public string PlayerName { get; set; } = string.Empty;
        public string NewRealm { get; set; } = string.Empty;
        public int NewLevel { get; set; }
        public int SoulRingNumber { get; set; }
    }

    /// <summary>Sự kiện đột phá thất bại — cơ thể không chịu nổi năng lượng Hồn Hoàn.</summary>
    public class OnBreakthroughFailedEvent
    {
        public string PlayerName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public float SuccessRate { get; set; }
    }

    /// <summary>Sự kiện nhân vật tử vong.</summary>
    public class OnPlayerDiedEvent
    {
        public string PlayerName { get; set; } = string.Empty;
        public string CauseOfDeath { get; set; } = string.Empty;
    }

    /// <summary>Sự kiện hấp thu Hồn Hoàn thành công.</summary>
    public class OnSoulRingAbsorbedEvent
    {
        public string PlayerName { get; set; } = string.Empty;
        public int RingNumber { get; set; }
        public int SoulBeastAge { get; set; }
        public string SoulSkillName { get; set; } = string.Empty;
    }

    /// <summary>Sự kiện thay đổi cảnh giới (danh hiệu).</summary>
    public class OnRealmChangedEvent
    {
        public string PlayerName { get; set; } = string.Empty;
        public string OldRealm { get; set; } = string.Empty;
        public string NewRealm { get; set; } = string.Empty;
        public int Level { get; set; }
    }

    /// <summary>Sự kiện tăng cấp Hồn Lực.</summary>
    public class OnLevelUpEvent
    {
        public string PlayerName { get; set; } = string.Empty;
        public int OldLevel { get; set; }
        public int NewLevel { get; set; }
    }

    /// <summary>Sự kiện chạm bình cảnh (nút thắt cần Hồn Hoàn).</summary>
    public class OnBottleneckReachedEvent
    {
        public string PlayerName { get; set; } = string.Empty;
        public int Level { get; set; }
        public string CurrentRealm { get; set; } = string.Empty;
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
