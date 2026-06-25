using System.Diagnostics;
using UnityEngine;

namespace GameTemplate.Core.Logger
{
    /// <summary>
    /// Logger có category, tự strip ở Release build để tránh tốn perf trên mobile.
    /// Dùng [Conditional] để compiler bỏ luôn các call này khi không define ENABLE_GAME_LOG.
    ///
    /// Setup trong Player Settings > Scripting Define Symbols:
    ///   - Development build: thêm ENABLE_GAME_LOG
    ///   - Release: bỏ ENABLE_GAME_LOG -> mọi call Log() sẽ bị compiler strip
    ///
    /// Cách dùng:
    ///   GameLog.Info(LogCategory.Audio, "Playing music: " + name);
    ///   GameLog.Warning(LogCategory.Save, "Save file corrupted, fallback to default");
    ///   GameLog.Error(LogCategory.Network, "Failed to fetch leaderboard");
    /// </summary>
    public static class GameLog
    {
        // Bitmask filter - bật/tắt từng category mà không cần đổi code
        public static LogCategory EnabledCategories = LogCategory.All;

        [Conditional("ENABLE_GAME_LOG")]
        public static void Info(LogCategory category, string message)
        {
            if ((EnabledCategories & category) == 0) return;
            UnityEngine.Debug.Log($"[{category}] {message}");
        }

        [Conditional("ENABLE_GAME_LOG")]
        public static void Warning(LogCategory category, string message)
        {
            if ((EnabledCategories & category) == 0) return;
            UnityEngine.Debug.LogWarning($"[{category}] {message}");
        }

        // Error không strip - lỗi production cần log để debug crash
        public static void Error(LogCategory category, string message)
        {
            UnityEngine.Debug.LogError($"[{category}] {message}");
        }
    }

    /// <summary>Bitmask categories - thêm category mới khi cần (vd: Multiplayer, Analytics).</summary>
    [System.Flags]
    public enum LogCategory
    {
        None       = 0,
        Bootstrap  = 1 << 0,
        Audio      = 1 << 1,
        Save       = 1 << 2,
        UI         = 1 << 3,
        Input      = 1 << 4,
        Scene      = 1 << 5,
        Gameplay   = 1 << 6,
        Network    = 1 << 7,
        Ads        = 1 << 8,
        IAP        = 1 << 9,
        Analytics  = 1 << 10,
        All        = ~0
    }
}
