using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GameTemplate.Core.Mobile.Localization;

namespace GameTemplate.Editor.AssetWorkflow
{
    /// <summary>
    /// CSV -> LocalizationTable importer.
    ///
    /// Format CSV mong đợi:
    ///   Key,English,Vietnamese,Japanese
    ///   ui.play,Play,Chơi,プレイ
    ///   ui.settings,Settings,Cài đặt,設定
    ///
    /// Hàng đầu = header: cột 1 phải là "Key", các cột sau là tên GameLanguage enum.
    ///
    /// Cách dùng:
    ///   1. GameTemplate > Import > CSV to Localization Table
    ///   2. Chọn file CSV
    ///   3. Chọn output path cho .asset
    ///   4. Designer sửa CSV trên Google Sheet, export CSV, re-import.
    /// </summary>
    public static class CsvToLocalizationImporter
    {
        [MenuItem("GameTemplate/Import/CSV to Localization Table", priority = 70)]
        public static void Import()
        {
            var csvPath = EditorUtility.OpenFilePanel("Chọn file CSV", "", "csv");
            if (string.IsNullOrEmpty(csvPath)) return;

            var outputPath = EditorUtility.SaveFilePanelInProject(
                "Save LocalizationTable",
                "LocalizationTable",
                "asset",
                "Chọn nơi save asset");
            if (string.IsNullOrEmpty(outputPath)) return;

            try
            {
                var table = ParseCsv(csvPath);
                AssetDatabase.CreateAsset(table, outputPath);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = table;
                Debug.Log($"[CSV Import] Imported {table.Entries.Length} entries vào {outputPath}");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Import Failed", ex.Message, "OK");
            }
        }

        private static LocalizationTable ParseCsv(string path)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length < 2)
                throw new System.Exception("CSV phải có ít nhất 2 hàng (header + 1 data).");

            // Parse header
            var headers = ParseCsvLine(lines[0]);
            if (headers[0].Trim() != "Key")
                throw new System.Exception("Cột đầu tiên phải tên là 'Key'.");

            // Map cột -> GameLanguage
            var languageColumns = new List<(int columnIndex, GameLanguage lang)>();
            for (int i = 1; i < headers.Count; i++)
            {
                var header = headers[i].Trim();
                if (System.Enum.TryParse<GameLanguage>(header, out var lang))
                    languageColumns.Add((i, lang));
                else
                    Debug.LogWarning($"[CSV Import] Cột '{header}' không match GameLanguage enum, skip.");
            }

            if (languageColumns.Count == 0)
                throw new System.Exception("Không có cột ngôn ngữ hợp lệ. Header phải dùng tên enum GameLanguage (English, Vietnamese, ...).");

            // Parse data
            var entries = new List<LocalizationTable.Entry>();
            for (int row = 1; row < lines.Length; row++)
            {
                if (string.IsNullOrWhiteSpace(lines[row])) continue;
                var cells = ParseCsvLine(lines[row]);
                if (cells.Count < headers.Count) continue;

                var entry = new LocalizationTable.Entry
                {
                    Key = cells[0].Trim(),
                    Values = languageColumns.Select(lc => new LocalizationTable.LanguageValue
                    {
                        Language = lc.lang,
                        Value = lc.columnIndex < cells.Count ? cells[lc.columnIndex] : ""
                    }).ToArray()
                };
                entries.Add(entry);
            }

            var table = ScriptableObject.CreateInstance<LocalizationTable>();
            table.Entries = entries.ToArray();
            return table;
        }

        /// <summary>
        /// Parse 1 dòng CSV xử lý: comma trong "...", escape "" -> "
        /// </summary>
        private static List<string> ParseCsvLine(string line)
        {
            var cells = new List<string>();
            var sb = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // Escape: "" -> "
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else sb.Append(c);
                }
                else
                {
                    if (c == ',')
                    {
                        cells.Add(sb.ToString());
                        sb.Clear();
                    }
                    else if (c == '"') inQuotes = true;
                    else sb.Append(c);
                }
            }
            cells.Add(sb.ToString());
            return cells;
        }
    }
}
