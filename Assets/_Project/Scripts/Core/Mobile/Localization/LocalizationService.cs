using System;
using System.Collections.Generic;
using UnityEngine;
using GameTemplate.Core.Logger;

namespace GameTemplate.Core.Mobile.Localization
{
    /// <summary>
    /// Enum ngôn ngữ của template - KHÔNG dùng tên SystemLanguage để tránh conflict với
    /// UnityEngine.SystemLanguage. Đây là enum riêng của template, list ngôn ngữ mình hỗ trợ.
    /// </summary>
    public enum GameLanguage
    {
        English, Vietnamese, Japanese, Korean, ChineseSimplified, ChineseTraditional,
        Spanish, French, German, Italian, Portuguese, Russian, Thai, Indonesian
    }

    /// <summary>
    /// Localization Service - tra cứu text theo key + ngôn ngữ.
    /// Dùng ScriptableObject làm database, không phụ thuộc plugin ngoài.
    ///
    /// Cách dùng:
    ///   1. Create > GameTemplate > Localization Table
    ///   2. Thêm Entry: key="ui.play", values cho từng ngôn ngữ
    ///   3. Trong code: var text = LocalizationService.Get("ui.play");
    ///   4. UI Text auto refresh nếu component dùng LocalizedText (tự viết theo project)
    /// </summary>
    public interface ILocalizationService
    {
        GameLanguage CurrentLanguage { get; }
        IReadOnlyList<GameLanguage> AvailableLanguages { get; }

        void SetLanguage(GameLanguage lang);
        string Get(string key, string fallback = null);

        /// <summary>Format string với placeholder: Get("score.text", 100) -> "Score: 100"</summary>
        string Get(string key, params object[] args);

        /// <summary>Event để UI subscribe và refresh khi đổi ngôn ngữ.</summary>
        event Action OnLanguageChanged;
    }

    [CreateAssetMenu(menuName = "GameTemplate/Localization/Localization Table", fileName = "LocalizationTable")]
    public class LocalizationTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string Key;
            public LanguageValue[] Values;
        }

        [Serializable]
        public class LanguageValue
        {
            public GameLanguage Language;
            [TextArea(1, 4)] public string Value;
        }

        public Entry[] Entries;
    }

    public class LocalizationService : ILocalizationService
    {
        private const string PrefsKey = "selected_language";

        private readonly Dictionary<GameLanguage, Dictionary<string, string>> _dict
            = new Dictionary<GameLanguage, Dictionary<string, string>>();

        private readonly List<GameLanguage> _available = new List<GameLanguage>();

        public GameLanguage CurrentLanguage { get; private set; } = GameLanguage.English;
        public IReadOnlyList<GameLanguage> AvailableLanguages => _available;

        public event Action OnLanguageChanged;

        public LocalizationService(LocalizationTable table, GameLanguage defaultLanguage = GameLanguage.English)
        {
            LoadTable(table);
            CurrentLanguage = LoadSavedLanguage(defaultLanguage);
            GameLog.Info(LogCategory.UI, $"Localization loaded ({_available.Count} langs, current: {CurrentLanguage})");
        }

        private GameLanguage LoadSavedLanguage(GameLanguage fallback)
        {
            if (PlayerPrefs.HasKey(PrefsKey))
            {
                var saved = PlayerPrefs.GetString(PrefsKey);
                if (Enum.TryParse<GameLanguage>(saved, out var lang) && _available.Contains(lang))
                    return lang;
            }

            // Tự detect ngôn ngữ device lần đầu
            var systemLang = DetectSystemLanguage();
            return _available.Contains(systemLang) ? systemLang : fallback;
        }

        private GameLanguage DetectSystemLanguage()
        {
            // Convert từ UnityEngine.SystemLanguage sang GameLanguage của mình.
            // Dùng full path UnityEngine.SystemLanguage để rõ ràng, không ambiguous.
            switch (Application.systemLanguage)
            {
                case UnityEngine.SystemLanguage.Vietnamese: return GameLanguage.Vietnamese;
                case UnityEngine.SystemLanguage.Japanese: return GameLanguage.Japanese;
                case UnityEngine.SystemLanguage.Korean: return GameLanguage.Korean;
                case UnityEngine.SystemLanguage.ChineseSimplified: return GameLanguage.ChineseSimplified;
                case UnityEngine.SystemLanguage.ChineseTraditional: return GameLanguage.ChineseTraditional;
                case UnityEngine.SystemLanguage.Spanish: return GameLanguage.Spanish;
                case UnityEngine.SystemLanguage.French: return GameLanguage.French;
                case UnityEngine.SystemLanguage.German: return GameLanguage.German;
                case UnityEngine.SystemLanguage.Italian: return GameLanguage.Italian;
                case UnityEngine.SystemLanguage.Portuguese: return GameLanguage.Portuguese;
                case UnityEngine.SystemLanguage.Russian: return GameLanguage.Russian;
                case UnityEngine.SystemLanguage.Thai: return GameLanguage.Thai;
                case UnityEngine.SystemLanguage.Indonesian: return GameLanguage.Indonesian;
                default: return GameLanguage.English;
            }
        }

        private void LoadTable(LocalizationTable table)
        {
            if (table == null || table.Entries == null) return;

            foreach (var entry in table.Entries)
            {
                foreach (var lv in entry.Values)
                {
                    if (!_dict.TryGetValue(lv.Language, out var langDict))
                    {
                        langDict = new Dictionary<string, string>();
                        _dict[lv.Language] = langDict;
                        _available.Add(lv.Language);
                    }
                    langDict[entry.Key] = lv.Value;
                }
            }
        }

        public void SetLanguage(GameLanguage lang)
        {
            if (!_available.Contains(lang))
            {
                GameLog.Warning(LogCategory.UI, $"Language {lang} không có trong table.");
                return;
            }
            if (CurrentLanguage == lang) return;

            CurrentLanguage = lang;
            PlayerPrefs.SetString(PrefsKey, lang.ToString());
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
            GameLog.Info(LogCategory.UI, $"Language changed: {lang}");
        }

        public string Get(string key, string fallback = null)
        {
            if (_dict.TryGetValue(CurrentLanguage, out var langDict))
            {
                if (langDict.TryGetValue(key, out var value))
                    return value;
            }
            // Fallback: thử English nếu khác
            if (CurrentLanguage != GameLanguage.English &&
                _dict.TryGetValue(GameLanguage.English, out var enDict) &&
                enDict.TryGetValue(key, out var enValue))
            {
                return enValue;
            }
            return fallback ?? $"[{key}]";
        }

        public string Get(string key, params object[] args)
        {
            var template = Get(key);
            try { return string.Format(template, args); }
            catch { return template; }
        }
    }
}
