using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameTemplate.Editor.Infrastructure
{
    /// <summary>
    /// Scene Auto-Loader - bấm Play ở bất cứ scene nào cũng tự load Bootstrap trước.
    /// Giải quyết vấn đề: dev đang sửa scene "Level_03" muốn test, bấm Play -> bị null reference
    /// vì các Manager chưa init (chỉ init ở Bootstrap scene).
    ///
    /// Cách hoạt động:
    ///   - Lưu scene đang mở
    ///   - Khi bấm Play -> EditorSceneManager load Bootstrap
    ///   - Bootstrap init xong sẽ tự load scene gameplay tương ứng
    ///   - Khi stop -> mở lại scene cũ
    ///
    /// Tự strip khỏi build vì namespace là Editor.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneAutoLoader
    {
        private const string EnabledKey = "GameTemplate_SceneAutoLoader_Enabled";
        private const string PreviousSceneKey = "GameTemplate_SceneAutoLoader_PreviousScene";
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, true);
            set => EditorPrefs.SetBool(EnabledKey, value);
        }

        static SceneAutoLoader()
        {
            // Subscribe play mode change
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!Enabled) return;

            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // Sắp Play -> lưu scene hiện tại, load Bootstrap
                var currentScene = EditorSceneManager.GetActiveScene().path;

                if (currentScene == BootstrapScenePath)
                    return; // đã ở Bootstrap rồi, không cần làm gì

                // Save scene cũ trước khi switch
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    // User cancel save -> abort play
                    EditorApplication.isPlaying = false;
                    return;
                }

                EditorPrefs.SetString(PreviousSceneKey, currentScene);

                if (System.IO.File.Exists(BootstrapScenePath))
                {
                    EditorSceneManager.OpenScene(BootstrapScenePath);
                }
                else
                {
                    Debug.LogWarning(
                        $"[SceneAutoLoader] Không tìm thấy {BootstrapScenePath}. " +
                        $"Tạo Bootstrap scene hoặc tắt auto-loader trong Tools menu.");
                }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                // Quit Play -> restore scene cũ
                var previous = EditorPrefs.GetString(PreviousSceneKey, "");
                if (!string.IsNullOrEmpty(previous) && System.IO.File.Exists(previous))
                {
                    EditorSceneManager.OpenScene(previous);
                    EditorPrefs.DeleteKey(PreviousSceneKey);
                }
            }
        }

        // Menu để bật/tắt
        [MenuItem("GameTemplate/Scene Auto-Loader/Enabled", priority = 100)]
        private static void ToggleEnabled() => Enabled = !Enabled;

        [MenuItem("GameTemplate/Scene Auto-Loader/Enabled", validate = true)]
        private static bool ToggleEnabledValidate()
        {
            Menu.SetChecked("GameTemplate/Scene Auto-Loader/Enabled", Enabled);
            return true;
        }
    }
}
