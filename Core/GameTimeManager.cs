using Microsoft.Xna.Framework;
using System;

namespace SandboxTuTien.Core
{
    /// <summary>
    /// Quản lý thời gian game theo tỷ lệ: 1 phút thực = 1 ngày trong game.
    /// Tỷ lệ quy đổi: x1440 (60 giây thực = 86400 giây game = 24 giờ game).
    /// </summary>
    public class GameTimeManager
    {
        // ====================================================================
        // CONSTANTS
        // ====================================================================

        /// <summary>Tỷ lệ thời gian: 1 phút thực = 1 ngày game → x1440.</summary>
        public const float TIME_SCALE = 1440f;

        /// <summary>Số giây game trong 1 ngày game (24h × 60m × 60s).</summary>
        private const float SECONDS_PER_GAME_DAY = 86400f;

        /// <summary>Số giây game trong 1 giờ game.</summary>
        private const float SECONDS_PER_GAME_HOUR = 3600f;

        // ====================================================================
        // STATE
        // ====================================================================

        /// <summary>Tổng số giây game đã trôi qua.</summary>
        private double _totalGameSeconds;

        /// <summary>Cờ tạm dừng thời gian (dùng khi pause menu, cutscene...).</summary>
        public bool IsPaused { get; set; }

        // ====================================================================
        // PROPERTIES (Read-Only)
        // ====================================================================

        /// <summary>Ngày game hiện tại (bắt đầu từ 1).</summary>
        public int GameDay => (int)(_totalGameSeconds / SECONDS_PER_GAME_DAY) + 1;

        /// <summary>Giờ trong ngày game hiện tại (0-23).</summary>
        public int GameHour => (int)((_totalGameSeconds % SECONDS_PER_GAME_DAY) / SECONDS_PER_GAME_HOUR);

        /// <summary>Phút trong giờ game hiện tại (0-59).</summary>
        public int GameMinute => (int)((_totalGameSeconds % SECONDS_PER_GAME_HOUR) / 60.0);

        /// <summary>Tổng số ngày game đã trôi qua (dạng float cho tính toán).</summary>
        public double TotalGameDays => _totalGameSeconds / SECONDS_PER_GAME_DAY;

        /// <summary>Tổng giây game (raw).</summary>
        public double TotalGameSeconds => _totalGameSeconds;

        // ====================================================================
        // TRACKING
        // ====================================================================

        private int _lastLoggedDay = -1;

        // ====================================================================
        // METHODS
        // ====================================================================

        /// <summary>
        /// Cập nhật thời gian game mỗi frame.
        /// Nhân deltaTime thực với TIME_SCALE để ra thời gian game.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (IsPaused) return;

            double realDeltaSeconds = gameTime.ElapsedGameTime.TotalSeconds;
            double gameDeltaSeconds = realDeltaSeconds * TIME_SCALE;

            _totalGameSeconds += gameDeltaSeconds;

            // Log khi chuyển sang ngày mới
            int currentDay = GameDay;
            if (currentDay != _lastLoggedDay)
            {
                _lastLoggedDay = currentDay;
                Console.WriteLine($"[Thế Giới] === Ngày {currentDay} ===  " +
                                  $"(Thời gian thực đã trôi: {_totalGameSeconds / TIME_SCALE:F1}s)");
            }
        }

        /// <summary>
        /// Trả về chuỗi hiển thị thời gian game (VD: "Ngày 5 - 14:30").
        /// </summary>
        public string GetFormattedTime()
        {
            return $"Ngày {GameDay} - {GameHour:D2}:{GameMinute:D2}";
        }

        /// <summary>Reset toàn bộ thời gian (dùng khi new game).</summary>
        public void Reset()
        {
            _totalGameSeconds = 0;
            _lastLoggedDay = -1;
        }
    }
}
