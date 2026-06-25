using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameTemplate.Editor.QualityOfLife
{
    /// <summary>
    /// Quick shortcuts cho các file/scene/folder hay truy cập.
    /// Đỡ phải tìm trong Project window mỗi lần.
    /// </summary>
    public static class ProjectShortcuts
    {
        [MenuItem("GameTemplate/Open/Bootstrap Scene", priority = 30)]
        public static void OpenBootstrap()
            => OpenScene("Assets/_Project/Scenes/Bootstrap.unity");

        [MenuItem("GameTemplate/Open/Main Menu Scene", priority = 31)]
        public static void OpenMainMenu()
            => OpenScene("Assets/_Project/Scenes/MainMenu.unity");

        [MenuItem("GameTemplate/Open/Build Settings", priority = 32)]
        public static void OpenBuildSettings()
            => EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));

        [MenuItem("GameTemplate/Open/Player Settings", priority = 33)]
        public static void OpenPlayerSettings()
            => SettingsService.OpenProjectSettings("Project/Player");

        [MenuItem("GameTemplate/Open/Quality Settings", priority = 34)]
        public static void OpenQualitySettings()
            => SettingsService.OpenProjectSettings("Project/Quality");

        [MenuItem("GameTemplate/Folder/Persistent Data Path (Saves)", priority = 50)]
        public static void OpenPersistentDataPath()
            => EditorUtility.RevealInFinder(Application.persistentDataPath + "/");

        [MenuItem("GameTemplate/Folder/Builds", priority = 51)]
        public static void OpenBuildsFolder()
        {
            var path = System.IO.Path.GetFullPath("Builds");
            if (System.IO.Directory.Exists(path))
                EditorUtility.RevealInFinder(path);
            else
                Debug.Log("Chưa có folder Builds. Build game trước.");
        }

        [MenuItem("GameTemplate/Clear/PlayerPrefs", priority = 60)]
        public static void ClearPlayerPrefs()
        {
            if (EditorUtility.DisplayDialog("Clear PlayerPrefs",
                "Xóa toàn bộ PlayerPrefs? (haptic setting, language, ...)",
                "Xóa", "Cancel"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("Cleared PlayerPrefs.");
            }
        }

        [MenuItem("GameTemplate/Clear/Save Files", priority = 61)]
        public static void ClearSaveFiles()
        {
            var savesDir = System.IO.Path.Combine(Application.persistentDataPath, "Saves");
            if (!System.IO.Directory.Exists(savesDir))
            {
                Debug.Log("Chưa có save nào.");
                return;
            }
            if (EditorUtility.DisplayDialog("Clear Save Files",
                $"Xóa toàn bộ file trong:\n{savesDir}",
                "Xóa", "Cancel"))
            {
                System.IO.Directory.Delete(savesDir, true);
                Debug.Log("Cleared saves.");
            }
        }

        private static void OpenScene(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning($"Không tìm thấy scene: {path}");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(path);
        }
    }
}
