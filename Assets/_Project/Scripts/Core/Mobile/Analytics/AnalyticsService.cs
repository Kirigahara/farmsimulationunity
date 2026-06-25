using GameTemplate.Core.Logger;
using System.Collections.Generic;
using System.Data.Common;

namespace GameTemplate.Core.Mobile.Analytics
{
    /// <summary>
    /// Analytics Service - log event để phân tích funnel, retention, behavior.
    ///
    /// Multi-provider design: 1 game có thể track lên nhiều platform cùng lúc
    /// (vd Firebase + GameAnalytics) - chỉ cần Register nhiều provider.
    ///
    /// Naming convention cho event (chuẩn Firebase):
    ///   - snake_case: level_start, level_complete, ad_shown
    ///   - max 40 ký tự, không space
    ///   - Param key cũng snake_case
    /// </summary>
    public interface IAnalyticsService
    {
        bool IsInitialized { get; }
        System.Threading.Tasks.Task<bool> InitializeAsync();

        void RegisterProvider(IAnalyticsProvider provider);
        void SetUserProperty(string key, string value);
        void SetUserId(string userId);

        // Track event đơn giản, không tham số
        void TrackEvent(string eventName);

        // Track event với tham số qua Dictionary (dễ mở rộng, nhưng phải convert kiểu thủ công trong provider)
        void TrackEvent(string eventName, Dictionary<string, object> parameters);

        // Track event với tham số qua params (dễ dùng, nhưng không mở rộng được kiểu phức tạp)
        //void TrackEvent(string eventName, params Parameter[] Param);

        // Helpers cho event chuẩn (đỡ typo)
        void TrackLevelStart(int levelIndex);
        void TrackLevelComplete(int levelIndex, float durationSeconds);
        void TrackLevelFail(int levelIndex, string reason);
        void TrackAdShown(string placement, string adType);
        void TrackPurchase(string productId, string currency, float price);
    }

    /// <summary>
    /// Provider plugin: 1 implement = 1 destination (Firebase, GameAnalytics, ...).
    /// AnalyticsService gửi event đến tất cả provider đã register.
    /// </summary>
    public interface IAnalyticsProvider
    {
        string Name { get; }
        void Initialize();
        void TrackEvent(string eventName, Dictionary<string, object> parameters);
        void SetUserProperty(string key, string value);
        void SetUserId(string userId);
    }

    /// <summary>
    /// Hub gửi event đến mọi provider đã đăng ký.
    /// </summary>
    public class AnalyticsService : IAnalyticsService
    {
        private readonly List<IAnalyticsProvider> _providers = new List<IAnalyticsProvider>();
        public bool IsInitialized { get; private set; }

        public System.Threading.Tasks.Task<bool> InitializeAsync()
        {
            foreach (var p in _providers)
            {
                try { p.Initialize(); }
                catch (System.Exception ex) { GameLog.Error(LogCategory.Analytics, $"Init '{p.Name}' fail: {ex}"); }
            }
            IsInitialized = true;
            return System.Threading.Tasks.Task.FromResult(true);
        }

        public void RegisterProvider(IAnalyticsProvider provider)
        {
            _providers.Add(provider);
            GameLog.Info(LogCategory.Analytics, $"Registered provider: {provider.Name}");
        }

        public void SetUserId(string userId)
        {
            foreach (var p in _providers) p.SetUserId(userId);
        }

        public void SetUserProperty(string key, string value)
        {
            foreach (var p in _providers) p.SetUserProperty(key, value);
        }

        public void TrackEvent(string eventName) => TrackEvent(eventName, null);

        public void TrackEvent(string eventName, Dictionary<string, object> parameters)
        {
            GameLog.Info(LogCategory.Analytics, $"Event: {eventName}");
            foreach (var p in _providers)
            {
                try { p.TrackEvent(eventName, parameters); }
                catch (System.Exception ex) { GameLog.Error(LogCategory.Analytics, $"Track event fail on '{p.Name}': {ex}"); }
            }
        }

        // === Helpers cho event chuẩn ===
        public void TrackLevelStart(int levelIndex)
            => TrackEvent("level_start", new Dictionary<string, object> { ["level"] = levelIndex });

        public void TrackLevelComplete(int levelIndex, float durationSeconds)
            => TrackEvent("level_complete", new Dictionary<string, object>
            {
                ["level"] = levelIndex,
                ["duration"] = durationSeconds
            });

        public void TrackLevelFail(int levelIndex, string reason)
            => TrackEvent("level_fail", new Dictionary<string, object>
            {
                ["level"] = levelIndex,
                ["reason"] = reason
            });

        public void TrackAdShown(string placement, string adType)
            => TrackEvent("ad_shown", new Dictionary<string, object>
            {
                ["placement"] = placement,
                ["ad_type"] = adType
            });

        public void TrackPurchase(string productId, string currency, float price)
            => TrackEvent("purchase", new Dictionary<string, object>
            {
                ["product_id"] = productId,
                ["currency"] = currency,
                ["price"] = price
            });
    }

    /// <summary>Provider mặc định khi chưa có SDK - chỉ log ra Console.</summary>
    public class MockAnalyticsProvider : IAnalyticsProvider
    {
        public string Name => "Mock";

        public void Initialize() => GameLog.Info(LogCategory.Analytics, "[Mock] Initialized.");

        public void TrackEvent(string eventName, Dictionary<string, object> parameters)
        {
            var paramStr = parameters == null ? "" : " " + DictToString(parameters);
            GameLog.Info(LogCategory.Analytics, $"[Mock] {eventName}{paramStr}");
        }

        public void SetUserProperty(string key, string value)
            => GameLog.Info(LogCategory.Analytics, $"[Mock] UserProperty {key}={value}");

        public void SetUserId(string userId)
            => GameLog.Info(LogCategory.Analytics, $"[Mock] UserId={userId}");

        private string DictToString(Dictionary<string, object> dict)
        {
            var sb = new System.Text.StringBuilder("{");
            foreach (var kv in dict) sb.Append($"{kv.Key}={kv.Value} ");
            sb.Append("}");
            return sb.ToString();
        }
    }

#if ANALYTICS_FIREBASE
    /// <summary>Firebase Analytics provider - chỉ tồn tại khi define ANALYTICS_FIREBASE.</summary>
    public class FirebaseAnalyticsProvider : IAnalyticsProvider
    {
        public string Name => "Firebase";

        public void Initialize()
        {
            // TODO: khi import Firebase SDK
            // Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            //     Firebase.Analytics.FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            // });
        }

        public void TrackEvent(string eventName, Dictionary<string, object> parameters)
        {
            // TODO:
            // if (parameters == null) {
            //     Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName);
            //     return;
            // }
            // var fbParams = new List<Firebase.Analytics.Parameter>();
            // foreach (var kv in parameters) {
            //     switch (kv.Value) {
            //         case string s: fbParams.Add(new Firebase.Analytics.Parameter(kv.Key, s)); break;
            //         case long l: fbParams.Add(new Firebase.Analytics.Parameter(kv.Key, l)); break;
            //         case int i: fbParams.Add(new Firebase.Analytics.Parameter(kv.Key, (long)i)); break;
            //         case double d: fbParams.Add(new Firebase.Analytics.Parameter(kv.Key, d)); break;
            //         case float f: fbParams.Add(new Firebase.Analytics.Parameter(kv.Key, (double)f)); break;
            //     }
            // }
            // Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName, fbParams.ToArray());
        }

        public void SetUserProperty(string key, string value)
        {
            // TODO: Firebase.Analytics.FirebaseAnalytics.SetUserProperty(key, value);
        }

        public void SetUserId(string userId)
        {
            // TODO: Firebase.Analytics.FirebaseAnalytics.SetUserId(userId);
        }
    }
#endif
}
