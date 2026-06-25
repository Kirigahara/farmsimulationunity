using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace GameTemplate.Editor.Infrastructure
{
    /// <summary>
    /// Define Symbol Manager - UI bật/tắt các define symbol của project.
    /// Thay vì gõ tay vào Project Settings (dễ typo, dễ quên).
    ///
    /// Khi tích vào checkbox -> tự thêm/xóa define vào Scripting Define Symbols
    /// của BuildTargetGroup hiện tại.
    /// </summary>
    public class DefineSymbolManagerWindow : EditorWindow
    {
        // Định nghĩa các define được quản lý.
        // Khi thêm SDK mới chỉ cần thêm 1 entry vào array này.
        private static readonly DefineGroup[] _groups = new[]
        {
            new DefineGroup("Logging", new[]
            {
                new DefineEntry("ENABLE_GAME_LOG", "Bật GameLog.Info/Warning (tắt để release)"),
            }),
            new DefineGroup("Ads", new[]
            {
                new DefineEntry("ADS_UNITY", "Unity Ads SDK"),
                new DefineEntry("ADS_ADMOB", "Google AdMob SDK"),
                new DefineEntry("ADS_APPLOVIN", "AppLovin MAX SDK"),
                new DefineEntry("ADS_IRONSOURCE", "IronSource SDK"),
            }),
            new DefineGroup("IAP", new[]
            {
                new DefineEntry("IAP_UNITY", "Unity IAP package"),
                new DefineEntry("IAP_GOOGLE_PLAY", "Google Play Billing native"),
            }),
            new DefineGroup("Analytics & Remote Config", new[]
            {
                new DefineEntry("ANALYTICS_FIREBASE", "Firebase Analytics"),
                new DefineEntry("ANALYTICS_GAMEANALYTICS", "GameAnalytics SDK"),
                new DefineEntry("REMOTE_CONFIG_FIREBASE", "Firebase Remote Config"),
            }),
        };

        private Vector2 _scroll;

        [MenuItem("GameTemplate/Define Symbol Manager", priority = 10)]
        public static void Open()
        {
            var window = GetWindow<DefineSymbolManagerWindow>("Define Symbols");
            window.minSize = new Vector2(400, 500);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Quản lý Scripting Define Symbols cho platform đang chọn.\n" +
                "Tick = thêm define, untick = xóa. Code có #if/#endif sẽ compile theo.",
                MessageType.Info);

            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            EditorGUILayout.LabelField($"Platform: {targetGroup}", EditorStyles.boldLabel);

            var currentDefines = GetDefines(targetGroup);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var group in _groups)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(group.GroupName, EditorStyles.boldLabel);

                foreach (var entry in group.Entries)
                {
                    bool current = currentDefines.Contains(entry.Symbol);

                    EditorGUILayout.BeginHorizontal();
                    bool newValue = EditorGUILayout.ToggleLeft(
                        new GUIContent(entry.Symbol, entry.Description),
                        current,
                        GUILayout.Width(220));
                    EditorGUILayout.LabelField(entry.Description, EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();

                    if (newValue != current)
                    {
                        if (newValue) currentDefines.Add(entry.Symbol);
                        else currentDefines.Remove(entry.Symbol);
                        SetDefines(targetGroup, currentDefines);
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            if (GUILayout.Button("Refresh", GUILayout.Height(30)))
            {
                Repaint();
            }
        }

        private static HashSet<string> GetDefines(BuildTargetGroup group)
        {
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(group);
            var defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            return new HashSet<string>(defines.Split(';', System.StringSplitOptions.RemoveEmptyEntries));
        }

        private static void SetDefines(BuildTargetGroup group, HashSet<string> defines)
        {
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(group);
            var joined = string.Join(";", defines);
            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, joined);
            Debug.Log($"[DefineManager] Updated defines: {joined}");
        }

        private class DefineGroup
        {
            public string GroupName;
            public DefineEntry[] Entries;
            public DefineGroup(string name, DefineEntry[] entries) { GroupName = name; Entries = entries; }
        }

        private class DefineEntry
        {
            public string Symbol;
            public string Description;
            public DefineEntry(string s, string d) { Symbol = s; Description = d; }
        }
    }
}
