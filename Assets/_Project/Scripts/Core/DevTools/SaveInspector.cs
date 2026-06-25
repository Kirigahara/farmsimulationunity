using System.IO;
using UnityEngine;

namespace GameTemplate.Core.DevTools
{
    // ====================================================================
    // SAVE INSPECTOR - xem/sửa save file ngay trong build dev.
    // Tự strip release build (giống CheatConsole).
    // ====================================================================

#if UNITY_EDITOR || DEVELOPMENT_BUILD

    /// <summary>
    /// Save Inspector - hiển thị overlay xem nội dung save file JSON.
    /// Cho phép sửa trực tiếp và save lại - test các edge case nhanh.
    ///
    /// Toggle: phím F2 trong Editor, hoặc 4-finger tap trên mobile dev build.
    /// </summary>
    public class SaveInspector : MonoBehaviour
    {
        private static SaveInspector _instance;
        private bool _isVisible;
        private string[] _saveFiles;
        private int _selectedIndex = -1;
        private string _editingContent = "";
        private Vector2 _scroll;
        private string _savesDir;
        private float _lastMultiTouchTime;

        public static void Show()
        {
            EnsureInstance();
            _instance._isVisible = true;
            _instance.RefreshFiles();
        }

        public static void Hide()
        {
            if (_instance != null) _instance._isVisible = false;
        }

        public static void Toggle()
        {
            EnsureInstance();
            _instance._isVisible = !_instance._isVisible;
            if (_instance._isVisible) _instance.RefreshFiles();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit() => EnsureInstance();

        private static void EnsureInstance()
        {
            if (_instance != null) return;
            var go = new GameObject("[SaveInspector]");
            go.hideFlags = HideFlags.HideAndDontSave;
            _instance = go.AddComponent<SaveInspector>();
            _instance._savesDir = Path.Combine(Application.persistentDataPath, "Saves");
            DontDestroyOnLoad(go);
        }

        private void Update()
        {
            if (Application.isEditor && Input.GetKeyDown(KeyCode.F2))
                Toggle();

#if UNITY_ANDROID || UNITY_IOS
            if (Input.touchCount >= 4 && Time.time - _lastMultiTouchTime > 1f)
            {
                _lastMultiTouchTime = Time.time;
                Toggle();
            }
#endif
        }

        private void RefreshFiles()
        {
            if (!Directory.Exists(_savesDir))
            {
                _saveFiles = new string[0];
                return;
            }
            _saveFiles = Directory.GetFiles(_savesDir, "*.json");
        }

        private void OnGUI()
        {
            if (!_isVisible) return;

            const float padding = 10f;
            float width = Screen.width - padding * 2;
            float height = Screen.height * 0.7f;

            GUI.Box(new Rect(padding, padding, width, height), "Save Inspector");

            GUILayout.BeginArea(new Rect(padding + 5, padding + 25, width - 10, height - 30));

            // Toolbar
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(80))) RefreshFiles();
            if (GUILayout.Button("Close", GUILayout.Width(80))) Hide();
            GUILayout.Label($"Path: {_savesDir}", GUI.skin.label);
            GUILayout.EndHorizontal();

            if (_saveFiles == null || _saveFiles.Length == 0)
            {
                GUILayout.Label("Chưa có save file nào.");
                GUILayout.EndArea();
                return;
            }

            // File list (top)
            GUILayout.Label("Files:");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < _saveFiles.Length; i++)
            {
                var fileName = Path.GetFileName(_saveFiles[i]);
                if (GUILayout.Toggle(_selectedIndex == i, fileName, "Button", GUILayout.Width(150)))
                {
                    if (_selectedIndex != i)
                    {
                        _selectedIndex = i;
                        try { _editingContent = File.ReadAllText(_saveFiles[i]); }
                        catch (System.Exception ex) { _editingContent = $"Lỗi đọc file: {ex.Message}"; }
                    }
                }
            }
            GUILayout.EndHorizontal();

            // Edit area
            if (_selectedIndex >= 0 && _selectedIndex < _saveFiles.Length)
            {
                _scroll = GUILayout.BeginScrollView(_scroll);
                _editingContent = GUILayout.TextArea(_editingContent, GUILayout.ExpandHeight(true));
                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Save Changes", GUILayout.Width(120)))
                {
                    try
                    {
                        File.WriteAllText(_saveFiles[_selectedIndex], _editingContent);
                        UnityEngine.Debug.Log($"[SaveInspector] Saved {_saveFiles[_selectedIndex]}");
                    }
                    catch (System.Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[SaveInspector] Save failed: {ex.Message}");
                    }
                }
                if (GUILayout.Button("Delete File", GUILayout.Width(120)))
                {
                    try
                    {
                        File.Delete(_saveFiles[_selectedIndex]);
                        _selectedIndex = -1;
                        RefreshFiles();
                    }
                    catch (System.Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[SaveInspector] Delete failed: {ex.Message}");
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }
    }

#else
    // Release: stub
    public static class SaveInspector
    {
        public static void Show() { }
        public static void Hide() { }
        public static void Toggle() { }
    }
#endif
}
