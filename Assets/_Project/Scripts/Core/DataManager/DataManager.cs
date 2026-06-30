using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using GameTemplate.Core.Save;
using GameTemplate.Core.DI;
using GameTemplate.Core.Logger;

namespace GameTemplate.Core.Data
{
    /// <summary>
    /// Generic Data Manager — nằm trong Core, không biết bất kỳ loại data gameplay nào.
    /// Gameplay tự tạo XxxService wrap lại DataManager để xử lý data cụ thể.
    ///
    /// Flow:
    ///   DataManager (Core) ← PlayerDataService (Gameplay)
    ///                       ← InventoryService  (Gameplay)
    ///                       ← QuestService      (Gameplay)
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        private readonly Dictionary<string, object> _cache = new();
        private ISaveService _saveService;
        public bool IsReady { get; private set; }

        public static float _ReduceGrassOnQuality = 1.0f;

        private void Awake()
        {
            _saveService = ServiceLocator.Get<ISaveService>();
            IsReady = true;
            GameLog.Info(LogCategory.Bootstrap, "[DataManager] Ready.");
        }

        /// <summary>
        /// Load data theo key. Nếu đã có trong cache thì trả về luôn, không đọc file lại.
        /// </summary>
        public async Task<T> LoadAsync<T>(string key) where T : class, new()
        {
            if (_cache.TryGetValue(key, out var cached))
                return (T)cached;

            var data = await _saveService.LoadAsync<T>(key);
            _cache[key] = data;
            GameLog.Info(LogCategory.Bootstrap, $"[DataManager] Loaded: {key}");
            return data;
        }

        /// <summary>
        /// Save data theo key. Cập nhật cache và ghi xuống file.
        /// </summary>
        public async Task SaveAsync<T>(string key, T data) where T : class, new()
        {
            _cache[key] = data;
            await _saveService.SaveAsync(key, data);
            GameLog.Info(LogCategory.Bootstrap, $"[DataManager] Saved: {key}");
        }
    }
}
