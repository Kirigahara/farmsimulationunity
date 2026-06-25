using System.Collections.Generic;
using System.Threading.Tasks;
using GameTemplate.Core.Logger;

namespace GameTemplate.Core.Mobile.RemoteConfig
{
    /// <summary>
    /// Remote Config - đổi balance/feature flag không cần update app.
    /// Use case:
    ///   - A/B test: 50% user thấy giá $0.99, 50% thấy $1.99
    ///   - Kill-switch: tắt feature bug khẩn cấp mà không cần submit update
    ///   - Tuning: chỉnh số coin/exp drop, tốc độ enemy, balance
    ///   - Soft launch: feature mới chỉ bật ở VN trước khi bật toàn cầu
    /// </summary>
    public interface IRemoteConfigService
    {
        bool IsInitialized { get; }
        Task<bool> FetchAsync();

        // Get values - luôn có default để safe khi fetch fail
        string GetString(string key, string defaultValue = "");
        int GetInt(string key, int defaultValue = 0);
        long GetLong(string key, long defaultValue = 0);
        float GetFloat(string key, float defaultValue = 0f);
        bool GetBool(string key, bool defaultValue = false);

        // Set default values - khi fetch chưa xong hoặc offline
        void SetDefaults(Dictionary<string, object> defaults);

        event System.Action OnConfigUpdated;
    }

    /// <summary>
    /// Mock Remote Config - lấy value từ dict in-memory.
    /// Dùng để test feature flag mà không cần Firebase console.
    /// </summary>
    public class MockRemoteConfigService : IRemoteConfigService
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        public bool IsInitialized { get; private set; }
        public event System.Action OnConfigUpdated;

        public async Task<bool> FetchAsync()
        {
            GameLog.Info(LogCategory.Network, "[Mock] RemoteConfig fetching...");
            await Task.Delay(300);
            IsInitialized = true;
            OnConfigUpdated?.Invoke();
            GameLog.Info(LogCategory.Network, $"[Mock] RemoteConfig fetched ({_values.Count} keys).");
            return true;
        }

        public void SetDefaults(Dictionary<string, object> defaults)
        {
            foreach (var kv in defaults)
                if (!_values.ContainsKey(kv.Key)) _values[kv.Key] = kv.Value;
        }

        /// <summary>Test helper: gán value runtime để giả lập config từ server.</summary>
        public void SetMockValue(string key, object value)
        {
            _values[key] = value;
            OnConfigUpdated?.Invoke();
        }

        public string GetString(string key, string defaultValue = "")
            => _values.TryGetValue(key, out var v) ? v.ToString() : defaultValue;

        public int GetInt(string key, int defaultValue = 0)
            => _values.TryGetValue(key, out var v) && int.TryParse(v.ToString(), out var i) ? i : defaultValue;

        public long GetLong(string key, long defaultValue = 0)
            => _values.TryGetValue(key, out var v) && long.TryParse(v.ToString(), out var l) ? l : defaultValue;

        public float GetFloat(string key, float defaultValue = 0f)
            => _values.TryGetValue(key, out var v) && float.TryParse(v.ToString(), out var f) ? f : defaultValue;

        public bool GetBool(string key, bool defaultValue = false)
            => _values.TryGetValue(key, out var v) && bool.TryParse(v.ToString(), out var b) ? b : defaultValue;
    }

    public static class RemoteConfigServiceFactory
    {
        public static IRemoteConfigService Create()
        {
#if REMOTE_CONFIG_FIREBASE && !UNITY_EDITOR
            return new FirebaseRemoteConfigService();
#else
            return new MockRemoteConfigService();
#endif
        }
    }

#if REMOTE_CONFIG_FIREBASE
    public class FirebaseRemoteConfigService : IRemoteConfigService
    {
        // TODO: implement với Firebase.RemoteConfig.FirebaseRemoteConfig
        // var config = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance;
        // await config.SetDefaultsAsync(defaults);
        // await config.FetchAsync(TimeSpan.Zero);
        // await config.ActivateAsync();
        public bool IsInitialized { get; private set; }
        public event System.Action OnConfigUpdated;
        public Task<bool> FetchAsync() => throw new System.NotImplementedException();
        public void SetDefaults(Dictionary<string, object> defaults) => throw new System.NotImplementedException();
        public string GetString(string k, string d = "") => d;
        public int GetInt(string k, int d = 0) => d;
        public long GetLong(string k, long d = 0) => d;
        public float GetFloat(string k, float d = 0f) => d;
        public bool GetBool(string k, bool d = false) => d;
    }
#endif
}
