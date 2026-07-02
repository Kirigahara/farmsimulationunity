using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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

        // ===== Mã hóa =====
        // Lưu ý: key/IV nhúng cứng trong build chỉ chặn được user sửa tay file save
        // bằng notepad/text editor. Không chống được người có kỹ năng reverse-engineer
        // app để lấy key. Nếu cần chống cheat nghiêm túc (leaderboard, PvP...), phải
        // validate ở server, không tin dữ liệu client.
        private const string EncryptionPassphrase = ",x%Bkh]&,5LYWEK.";//Key được gán 1 lần duy nhất, sẽ không được đổi khi build các version tiếp theo
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("GameTemplate.Core.Save.Salt");

        public JsonSaveService()
        {
            _baseDir = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(_baseDir))
                Directory.CreateDirectory(_baseDir);
        }

        private string GetPath(string key) => Path.Combine(_baseDir, key + FileExtension);

        private static (byte[] key, byte[] iv) DeriveKeyIv()
        {
            using var deriveBytes = new Rfc2898DeriveBytes(
                EncryptionPassphrase, Salt, 10000, HashAlgorithmName.SHA256);
            byte[] key = deriveBytes.GetBytes(32); // AES-256
            byte[] iv = deriveBytes.GetBytes(16);
            return (key, iv);
        }

        private static string Encrypt(string plainText)
        {
            var (key, iv) = DeriveKeyIv();
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            return Convert.ToBase64String(cipherBytes);
        }

        private static string Decrypt(string cipherTextBase64)
        {
            var (key, iv) = DeriveKeyIv();
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor();
            byte[] cipherBytes = Convert.FromBase64String(cipherTextBase64);
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

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
                string raw = await Task.Run(() => File.ReadAllText(path));
                string json;
                try
                {
                    json = Decrypt(raw);
                }
                catch (Exception decryptEx)
                {
                    // Có thể là save cũ chưa mã hóa (từ bản build trước) — fallback đọc thẳng JSON.
                    GameLog.Warning(LogCategory.Save, $"Decrypt '{key}' failed ({decryptEx.Message}), thử đọc như JSON thường.");
                    json = raw;
                }
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
                string encrypted = Encrypt(json);
                // Atomic write: tránh corrupt khi user kill app giữa chừng
                await Task.Run(() =>
                {
                    File.WriteAllText(tempPath, encrypted);
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(tempPath, path);
                });
                GameLog.Info(LogCategory.Save, $"Saved '{key}' ({json.Length} bytes, encrypted).");
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
