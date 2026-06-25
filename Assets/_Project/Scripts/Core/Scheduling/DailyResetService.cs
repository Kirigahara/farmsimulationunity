using System;
using UnityEngine;
using GameTemplate.Core.Logger;

namespace GameTemplate.Core.Scheduling
{
    /// <summary>
    /// Service check "đã sang ngày mới chưa" - dùng cho daily reward, daily quest, daily login bonus...
    ///
    /// TRIẾT LÝ THIẾT KẾ:
    ///   - CHỈ làm phần universal: check ngày, lưu lần claim cuối, đếm streak
    ///   - KHÔNG làm: UI claim, reward data, apply reward (mỗi game khác nhau)
    ///   - Dùng device time (DateTime.Now) - chấp nhận bị hack nếu user chỉnh đồng hồ
    ///   - Persist qua PlayerPrefs (đơn giản, đủ dùng cho daily timestamp)
    ///
    /// MULTI-KEY:
    ///   1 service quản nhiều "daily key" độc lập:
    ///   - "login_bonus"    → daily login reward
    ///   - "daily_quest"    → daily quest
    ///   - "free_chest"     → free chest opening
    ///   - "ad_watch_limit" → reset số ad watch hàng ngày
    ///
    /// CÁCH DÙNG (ở LoginBonusController của game):
    ///   var daily = ServiceLocator.Get&lt;IDailyResetService&gt;();
    ///
    ///   if (daily.IsNewDay("login_bonus"))
    ///   {
    ///       int streak = daily.GetStreakIfClaimed("login_bonus");
    ///       ShowClaimUI(streak);  // game tự handle reward data + UI
    ///   }
    ///
    ///   // Khi user bấm claim:
    ///   daily.MarkAsClaimed("login_bonus");
    ///   ApplyReward(streak);     // game tự apply
    ///
    /// HACK PROTECTION:
    ///   Mặc định CHẤP NHẬN hack (user chỉnh device time). Nếu cần chống hack:
    ///   1. Dùng server time (cần backend)
    ///   2. Hoặc dùng NTP API (Google/Apple time server)
    ///   3. Hoặc check "thời gian chỉ tiến không lùi" → if (now < lastClaim) → cheating
    /// </summary>
    public interface IDailyResetService
    {
        /// <summary>
        /// Hôm nay đã qua ngày so với lần claim cuối chưa?
        /// True = chưa từng claim HOẶC ngày hiện tại khác ngày lần claim cuối.
        /// </summary>
        bool IsNewDay(string key);

        /// <summary>
        /// Đánh dấu "đã claim hôm nay" - lưu timestamp hiện tại + tăng streak nếu liên tiếp.
        /// </summary>
        void MarkAsClaimed(string key);

        /// <summary>
        /// DateTime lần claim cuối. Return DateTime.MinValue nếu chưa từng claim.
        /// </summary>
        DateTime GetLastClaimDate(string key);

        /// <summary>
        /// Số ngày streak hiện tại (đếm sau lần claim cuối). 0 nếu đã đứt streak hoặc chưa claim.
        /// Vd: claim 5 ngày liên tiếp → return 5. Bỏ 1 ngày → streak reset về 0 ở lần claim tới.
        /// </summary>
        int GetStreakIfClaimed(string key);

        /// <summary>
        /// Thời gian còn lại tới ngày tiếp theo (00:00 hôm sau).
        /// Dùng để hiện countdown "Next reward in 03:25:10".
        /// </summary>
        TimeSpan GetTimeUntilNextReset();

        /// <summary>Reset toàn bộ data của 1 key (dùng cho cheat / test).</summary>
        void ResetKey(string key);
    }

    /// <summary>
    /// Implementation PlayerPrefs - đơn giản, đủ dùng cho daily timestamp.
    /// Nếu cần persist phức tạp hơn (vd: lưu cùng SavedCharacterStats), implement bản khác.
    /// </summary>
    public class DailyResetService : IDailyResetService
    {
        // PlayerPrefs key prefix
        private const string LAST_CLAIM_PREFIX = "Daily_LastClaim_";
        private const string STREAK_PREFIX = "Daily_Streak_";

        // Format ISO 8601 - parse được không phụ thuộc culture
        private const string DATE_FORMAT = "yyyy-MM-dd";

        // ============================================================
        // IS NEW DAY
        // ============================================================
        public bool IsNewDay(string key)
        {
            var lastClaim = GetLastClaimDate(key);

            // Chưa từng claim → coi như ngày mới
            if (lastClaim == DateTime.MinValue) return true;

            // So sánh DATE only (không phải full DateTime)
            // Hôm nay là 2026-05-25, last claim là 2026-05-24 → IsNewDay = true
            return DateTime.Now.Date > lastClaim.Date;
        }

        // ============================================================
        // MARK AS CLAIMED + STREAK TRACKING
        // ============================================================
        public void MarkAsClaimed(string key)
        {
            var now = DateTime.Now;
            var lastClaim = GetLastClaimDate(key);

            // Tính streak mới
            int newStreak;
            if (lastClaim == DateTime.MinValue)
            {
                // Lần claim đầu tiên
                newStreak = 1;
            }
            else
            {
                // Tính số ngày giữa last claim và bây giờ (theo DATE, không phải tiếng)
                int daysDiff = (int)(now.Date - lastClaim.Date).TotalDays;

                if (daysDiff == 1)
                {
                    // Claim hôm qua → streak +1
                    newStreak = GetStreakIfClaimed(key) + 1;
                }
                else if (daysDiff == 0)
                {
                    // Đã claim hôm nay rồi (hiếm gặp - logic game không cho claim 2 lần/ngày)
                    GameLog.Warning(LogCategory.Save,
                        $"[DailyReset] Key '{key}' đã claim hôm nay rồi - skip MarkAsClaimed.");
                    return;
                }
                else
                {
                    // Đứt streak (bỏ >= 1 ngày)
                    newStreak = 1;
                }
            }

            // Save
            PlayerPrefs.SetString(LAST_CLAIM_PREFIX + key, now.ToString(DATE_FORMAT));
            PlayerPrefs.SetInt(STREAK_PREFIX + key, newStreak);
            PlayerPrefs.Save();

            GameLog.Info(LogCategory.Save,
                $"[DailyReset] '{key}' claimed. Date: {now:yyyy-MM-dd}, Streak: {newStreak}");
        }

        // ============================================================
        // GETTERS
        // ============================================================
        public DateTime GetLastClaimDate(string key)
        {
            string stored = PlayerPrefs.GetString(LAST_CLAIM_PREFIX + key, "");
            if (string.IsNullOrEmpty(stored)) return DateTime.MinValue;

            if (DateTime.TryParseExact(stored, DATE_FORMAT,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var parsed))
            {
                return parsed;
            }
            return DateTime.MinValue;
        }

        public int GetStreakIfClaimed(string key)
        {
            return PlayerPrefs.GetInt(STREAK_PREFIX + key, 0);
        }

        public TimeSpan GetTimeUntilNextReset()
        {
            var now = DateTime.Now;
            var nextReset = now.Date.AddDays(1); // 00:00 ngày mai
            return nextReset - now;
        }

        // ============================================================
        // RESET (cho cheat / test)
        // ============================================================
        public void ResetKey(string key)
        {
            PlayerPrefs.DeleteKey(LAST_CLAIM_PREFIX + key);
            PlayerPrefs.DeleteKey(STREAK_PREFIX + key);
            PlayerPrefs.Save();
            GameLog.Info(LogCategory.Save, $"[DailyReset] Reset key '{key}'");
        }
    }
}
