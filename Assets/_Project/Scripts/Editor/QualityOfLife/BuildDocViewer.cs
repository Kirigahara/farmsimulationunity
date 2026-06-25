using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GameTemplate.Editor.QualityOfLife
{
    /// <summary>
    /// Build HTML viewer cho documentation - render markdown đẹp như GitHub Pages.
    ///
    /// 100% offline: load JS library từ Documentation~/vendors/ folder, không cần internet.
    ///
    /// Cách hoạt động:
    /// 1. Đọc template HTML (viewer-template.html)
    /// 2. Đọc mọi file .md trong Documentation~/
    /// 3. Inject nội dung markdown vào template
    /// 4. Output file Documentation~/index.html
    ///
    /// Workflow:
    /// - Sửa file .md → menu "Rebuild HTML Viewer" → menu "Open in Browser"
    /// </summary>
    public static class BuildDocViewer
    {
        private const string DocsFolder = "Documentation~";
        private const string TemplateFile = "viewer-template.html";
        private const string VendorsFolder = "vendors";
        private const string OutputFile = "index.html";

        [MenuItem("GameTemplate/Documentation/🔨 Rebuild HTML Viewer", priority = 100)]
        public static void Rebuild()
        {
            try
            {
                var docsPath = Path.GetFullPath(DocsFolder);
                if (!Directory.Exists(docsPath))
                {
                    EditorUtility.DisplayDialog("Error",
                        $"Folder không tồn tại: {docsPath}", "OK");
                    return;
                }

                var templatePath = Path.Combine(docsPath, TemplateFile);
                if (!File.Exists(templatePath))
                {
                    EditorUtility.DisplayDialog("Error",
                        $"Template thiếu: {templatePath}\n\nCopy lại file viewer-template.html từ template.",
                        "OK");
                    return;
                }

                // Check vendors folder
                var vendorsPath = Path.Combine(docsPath, VendorsFolder);
                if (!Directory.Exists(vendorsPath))
                {
                    var ok = EditorUtility.DisplayDialog(
                        "Thiếu vendors folder",
                        $"Folder vendors/ không tồn tại tại:\n{vendorsPath}\n\n" +
                        "Folder này chứa JS library (marked.js, mermaid.js, highlight.js) " +
                        "để render markdown offline.\n\n" +
                        "Bạn vẫn muốn build? (HTML sẽ không render được khi mở)",
                        "Build anyway", "Cancel");
                    if (!ok) return;
                }
                else
                {
                    // Verify file vendors có đủ không
                    var requiredFiles = new[] {
                        "marked.min.js", "mermaid.min.js",
                        "highlight.min.js", "github-dark.min.css"
                    };
                    var missing = new System.Collections.Generic.List<string>();
                    foreach (var f in requiredFiles)
                    {
                        if (!File.Exists(Path.Combine(vendorsPath, f)))
                            missing.Add(f);
                    }
                    if (missing.Count > 0)
                    {
                        EditorUtility.DisplayDialog("Vendors thiếu file",
                            "Folder vendors/ thiếu các file sau:\n\n" +
                            string.Join("\n", missing) + "\n\n" +
                            "HTML viewer sẽ không render đúng. Copy đủ file vendors từ template.",
                            "OK");
                    }
                }

                // Build
                var template = File.ReadAllText(templatePath);
                var markdownFiles = Directory.GetFiles(docsPath, "*.md");

                if (markdownFiles.Length == 0)
                {
                    EditorUtility.DisplayDialog("Warning",
                        "Không có file .md nào trong Documentation/", "OK");
                    return;
                }

                var content = BuildMarkdownJson(markdownFiles);
                var injection = $"window.__MARKDOWN_CONTENT__ = {content};";
                var injected = template.Replace(
                    "const MARKDOWN_CONTENT = window.__MARKDOWN_CONTENT__ || {};",
                    $"{injection}\n  const MARKDOWN_CONTENT = window.__MARKDOWN_CONTENT__;"
                );

                var outputPath = Path.Combine(docsPath, OutputFile);
                File.WriteAllText(outputPath, injected);

                var sizeMb = new FileInfo(outputPath).Length / 1024f;
                Debug.Log($"[Docs] Built HTML viewer: {outputPath} ({sizeMb:F1} KB, {markdownFiles.Length} files)");
                EditorUtility.DisplayDialog("Build Success",
                    $"HTML viewer build xong!\n\n" +
                    $"📄 {markdownFiles.Length} markdown files\n" +
                    $"📦 Size: {sizeMb:F1} KB\n" +
                    $"📂 {outputPath}\n\n" +
                    $"Mở qua menu: GameTemplate → Documentation → 🌐 Open in Browser",
                    "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Docs] Build failed: {ex}");
                EditorUtility.DisplayDialog("Build Failed", ex.Message, "OK");
            }
        }

        [MenuItem("GameTemplate/Documentation/🌐 Open in Browser", priority = 101)]
        public static void OpenInBrowser()
        {
            var indexPath = Path.GetFullPath(Path.Combine(DocsFolder, OutputFile));
            if (!File.Exists(indexPath))
            {
                if (EditorUtility.DisplayDialog("HTML chưa build",
                    "File index.html chưa tồn tại. Build ngay?",
                    "Build & Open", "Cancel"))
                {
                    Rebuild();
                    if (!File.Exists(indexPath)) return;
                }
                else return;
            }

            Application.OpenURL("file://" + indexPath.Replace("\\", "/"));
        }

        // ==================== Helpers ====================

        private static string BuildMarkdownJson(string[] markdownFiles)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            bool first = true;
            foreach (var file in markdownFiles)
            {
                var fileName = Path.GetFileName(file);
                if (fileName == OutputFile) continue;

                if (!first) sb.AppendLine(",");
                first = false;

                var content = File.ReadAllText(file);
                var escaped = EscapeJsonString(content);
                sb.Append("    \"").Append(fileName).Append("\": \"").Append(escaped).Append("\"");
            }
            sb.AppendLine();
            sb.Append("  }");
            return sb.ToString();
        }

        private static string EscapeJsonString(string s)
        {
            var sb = new StringBuilder(s.Length + 32);
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20)
                            sb.AppendFormat("\\u{0:x4}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
