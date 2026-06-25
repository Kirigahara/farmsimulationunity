using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using GameTemplate.Core.Logger;

namespace GameTemplate.Core.Save
{
    public interface ISaveService
    {
        Task<T> LoadAsync<T>(string key) where T : class, new();
        Task SaveAsync<T>(string key, T data) where T : class;
        bool HasSave(string key);
        void Delete(string key);
    }

    /// <summary>
    /// Save Service dùng JSON + persistentDataPath, an toàn cho mobile (iOS sandbox + Android scoped storage).
    ///
    /// Lưu ý mobile:
    ///   - persistentDataPath là path duy nhất ổn định trên cả 2 platform.
    ///   - Không dùng Application.dataPath (read-only trên build).
    ///   - SaveAsync chạy thread khác để không freeze main thread khi save lớn.
    ///   - Atomic write: ghi vào file tạm rồi rename, tránh corrupt khi user kill app giữa chừng.
    /// </summary>
    public class JsonSaveService : ISaveService
    {
        private readonly string _baseDir;
        private const string FileExtension = ".json";

        public JsonSaveService()
        {
            _baseDir = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(_baseDir))
                Directory.CreateDirectory(_baseDir);
        }

        private string GetPath(string key) => Path.Combine(_baseDir, key + FileExtension);

        public async Task<T> LoadAsync<T>(string key) where T : class, new()
        {
            var path = GetPath(key);
            if (!File.Exists(path))
            {
                GameLog.Info(LogCategory.Save, $"No save at '{key}', return default.");
                return new T();
            }

            try
            {
                string json = await Task.Run(() => File.ReadAllText(path));
                var data = JsonUtility.FromJson<T>(json);
                if (data == null)
                {
                    GameLog.Warning(LogCategory.Save, $"Save '{key}' parsed null, return default.");
                    return new T();
                }
                return data;
            }
            catch (Exception ex)
            {
                GameLog.Error(LogCategory.Save, $"Load '{key}' failed: {ex.Message}. Return default.");
                return new T();
            }
        }

        public async Task SaveAsync<T>(string key, T data) where T : class
        {
            if (data == null) return;

            var path = GetPath(key);
            var tempPath = path + ".tmp";

            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: false);
                // Atomic write: tránh corrupt khi user kill app giữa chừng
                await Task.Run(() =>
                {
                    File.WriteAllText(tempPath, json);
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(tempPath, path);
                });
                GameLog.Info(LogCategory.Save, $"Saved '{key}' ({json.Length} bytes).");
            }
            catch (Exception ex)
            {
                GameLog.Error(LogCategory.Save, $"Save '{key}' failed: {ex.Message}");
                if (File.Exists(tempPath))
                    try { File.Delete(tempPath); } catch { }
            }
        }

        public bool HasSave(string key) => File.Exists(GetPath(key));

        public void Delete(string key)
        {
            var path = GetPath(key);
            if (File.Exists(path))
            {
                File.Delete(path);
                GameLog.Info(LogCategory.Save, $"Deleted save '{key}'.");
            }
        }
    }

    /// <summary>
    /// Base class cho mọi save data - có version để migrate khi đổi schema.
    /// Khi đổi struct, tăng SaveVersion và xử lý migration trong code load.
    /// </summary>
    [Serializable]
    public abstract class SaveDataBase
    {
        public int SaveVersion = 1;
    }
}
