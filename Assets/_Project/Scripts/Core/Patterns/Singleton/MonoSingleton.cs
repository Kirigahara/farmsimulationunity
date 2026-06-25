using UnityEngine;

namespace GameTemplate.Core.Patterns.Singleton
{
    /// <summary>
    /// Singleton MonoBehaviour an toàn cho Unity.
    ///
    /// LƯU Ý QUAN TRỌNG:
    ///   - Template này ƯU TIÊN ServiceLocator hơn Singleton.
    ///   - Chỉ dùng Singleton khi: service không có interface, hoặc dev quen Singleton pattern,
    ///     hoặc khi muốn truy cập từ static context.
    ///   - Đừng abuse - mỗi Singleton thêm = 1 điểm coupling toàn project.
    ///
    /// Tính năng:
    ///   - Lazy: chỉ tạo khi Instance được gọi lần đầu
    ///   - Auto find existing trong scene trước khi tạo mới
    ///   - DontDestroyOnLoad tự động
    ///   - Tránh "ghost singleton" khi quit Play Mode (_quitting flag)
    ///
    /// Cách dùng:
    ///   public class GameManager : MonoSingleton<GameManager> { ... }
    ///   GameManager.Instance.StartGame();
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private static bool _quitting;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                // Khi đang quit Play, đừng tạo instance mới (sẽ leak GameObject)
                if (_quitting)
                {
                    Debug.LogWarning($"[Singleton] {typeof(T).Name} đang quit, return null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance != null) return _instance;

                    // Tìm trong scene trước
                    // Unity 2023+: dùng FindAnyObjectByType (nhanh hơn, không sort hierarchy).
                    // Unity 2022 trở xuống: fallback FindObjectOfType.
#if UNITY_2023_1_OR_NEWER
                    _instance = FindAnyObjectByType<T>();
#else
                    _instance = FindObjectOfType<T>();
#endif
                    if (_instance != null) return _instance;

                    // Tạo mới nếu không có
                    var go = new GameObject($"[Singleton] {typeof(T).Name}");
                    _instance = go.AddComponent<T>();
                    DontDestroyOnLoad(go);
                    return _instance;
                }
            }
        }

        public static bool HasInstance => _instance != null && !_quitting;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[Singleton] Duplicate {typeof(T).Name}, destroy.");
                Destroy(gameObject);
                return;
            }
            _instance = (T)this;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnApplicationQuit()
        {
            _quitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }

    /// <summary>
    /// Singleton pure C# (không phải MonoBehaviour) - lazy, thread-safe.
    /// Dùng cho service không cần Unity lifecycle (vd: NetworkClient, AnalyticsTracker logic).
    ///
    /// Cách dùng:
    ///   public class ConfigCache : Singleton<ConfigCache>
    ///   {
    ///       protected ConfigCache() { /* ctor private để force qua Instance */ }
    ///   }
    /// </summary>
    public abstract class Singleton<T> where T : class, new()
    {
        // .NET tự đảm bảo Lazy<T> thread-safe (default mode: ExecutionAndPublication)
        private static readonly System.Lazy<T> _lazy = new System.Lazy<T>(() => new T());

        public static T Instance => _lazy.Value;
    }
}
