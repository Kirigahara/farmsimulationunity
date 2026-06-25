using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameTemplate.Editor.QualityOfLife
{
    /// <summary>
    /// Mở documentation từ menu Unity.
    ///
    /// 3 cách mở:
    /// 1. 🌐 Browser (HTML viewer): đẹp như GitHub Pages, có sidebar + Mermaid + search.
    ///    Yêu cầu chạy "Rebuild HTML Viewer" trước (1 lần / sau khi sửa doc).
    ///
    /// 2. 📝 Local file .md: mở bằng app mặc định (VSCode/Typora/Notepad).
    ///    Nhanh, không cần build.
    ///
    /// 3. ☁️ GitHub: mở URL repo trên GitHub - render Mermaid, search built-in.
    ///    Cần internet và push lên repo.
    /// </summary>
    public static class OpenDocumentation
    {
        // ⚠️ Đổi URL này thành URL repo của team
        private const string GitHubBaseUrl =
            "https://github.com/LutechHieuUnityDev/LutechUnityTemplateAll/tree/main/UnityDevelopTemplate/Documentation~/";

        private const string DocsFolder = "Documentation~";

        // ==================== Browser (HTML viewer) ====================
        // Mở index.html với hash #FILE.md để jump thẳng vào file

        [MenuItem("GameTemplate/Documentation/🌐 Browser/📖 Cookbook", priority = 150)]
        public static void OpenCookbookBrowser() => OpenBrowser("COOKBOOK.md");

        [MenuItem("GameTemplate/Documentation/🌐 Browser/📊 Diagrams", priority = 151)]
        public static void OpenDiagramsBrowser() => OpenBrowser("DIAGRAMS.md");

        [MenuItem("GameTemplate/Documentation/🌐 Browser/🔍 API Reference", priority = 152)]
        public static void OpenApiBrowser() => OpenBrowser("API_REFERENCE.md");

        [MenuItem("GameTemplate/Documentation/🌐 Browser/🎓 Onboarding", priority = 153)]
        public static void OpenOnboardingBrowser() => OpenBrowser("ONBOARDING.md");

        [MenuItem("GameTemplate/Documentation/🌐 Browser/🏗 Architecture", priority = 154)]
        public static void OpenArchitectureBrowser() => OpenBrowser("ARCHITECTURE.md");

        [MenuItem("GameTemplate/Documentation/🌐 Browser/🎨 Patterns", priority = 155)]
        public static void OpenPatternsBrowser() => OpenBrowser("PATTERNS.md");

        [MenuItem("GameTemplate/Documentation/🌐 Browser/📱 Mobile Integration", priority = 156)]
        public static void OpenMobileBrowser() => OpenBrowser("MOBILE_INTEGRATION.md");

        [MenuItem("GameTemplate/Documentation/🌐 Browser/🔧 Editor Tools", priority = 157)]
        public static void OpenEditorBrowser() => OpenBrowser("EDITOR_TOOLS.md");

        // ==================== Local files (mở bằng app mặc định) ====================

        [MenuItem("GameTemplate/Documentation/📝 Local file/📖 Cookbook", priority = 200)]
        public static void OpenCookbookLocal() => OpenLocal("COOKBOOK.md");

        [MenuItem("GameTemplate/Documentation/📝 Local file/📊 Diagrams", priority = 201)]
        public static void OpenDiagramsLocal() => OpenLocal("DIAGRAMS.md");

        [MenuItem("GameTemplate/Documentation/📝 Local file/🔍 API Reference", priority = 202)]
        public static void OpenApiLocal() => OpenLocal("API_REFERENCE.md");

        [MenuItem("GameTemplate/Documentation/📝 Local file/📂 Open Docs Folder", priority = 230)]
        public static void OpenDocsFolder()
        {
            var folder = Path.GetFullPath(DocsFolder);
            if (Directory.Exists(folder))
                EditorUtility.RevealInFinder(folder);
            else
                Debug.LogWarning($"[Docs] Folder không tồn tại: {folder}");
        }

        // ==================== GitHub URLs ====================

        [MenuItem("GameTemplate/Documentation/☁️ GitHub/📖 Cookbook", priority = 250)]
        public static void OpenCookbookWeb() => OpenWeb("COOKBOOK.md");

        [MenuItem("GameTemplate/Documentation/☁️ GitHub/📊 Diagrams", priority = 251)]
        public static void OpenDiagramsWeb() => OpenWeb("DIAGRAMS.md");

        [MenuItem("GameTemplate/Documentation/☁️ GitHub/🔍 API Reference", priority = 252)]
        public static void OpenApiWeb() => OpenWeb("API_REFERENCE.md");

        // ==================== Implementation ====================

        /// <summary>Mở HTML viewer trong browser, jump tới file qua hash URL.</summary>
        private static void OpenBrowser(string mdFileName)
        {
            var indexPath = Path.GetFullPath(Path.Combine(DocsFolder, "index.html"));
            if (!File.Exists(indexPath))
            {
                if (EditorUtility.DisplayDialog("HTML chưa build",
                    "Cần build HTML viewer trước khi mở.\n\n" +
                    "Build ngay? (sẽ chạy 'Rebuild HTML Viewer' rồi mở)",
                    "Build & Open", "Cancel"))
                {
                    BuildDocViewer.Rebuild();
                    if (!File.Exists(indexPath)) return;
                }
                else return;
            }

            // file:// URL với hash để HTML viewer auto-load file đúng
            var url = "file://" + indexPath.Replace("\\", "/") + "#" + mdFileName;
            Application.OpenURL(url);
        }

        /// <summary>Mở file .md bằng app mặc định của OS (VSCode/Typora/Notepad).</summary>
        private static void OpenLocal(string fileName)
        {
            var path = Path.GetFullPath(Path.Combine(DocsFolder, fileName));
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[Docs] Không tìm thấy: {path}");
                return;
            }
            Application.OpenURL("file://" + path);
        }

        private static void OpenWeb(string fileName)
        {
            Application.OpenURL(GitHubBaseUrl + fileName);
        }
    }
}
