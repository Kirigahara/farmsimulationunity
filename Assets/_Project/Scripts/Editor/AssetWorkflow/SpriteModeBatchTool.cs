using UnityEditor;
using UnityEngine;

namespace GameTemplate.Editor.AssetWorkflow
{
    /// <summary>
    /// Tool batch convert Sprite Mode = Multiple → Single cho ảnh ĐÃ import rồi.
    ///
    /// AssetPostprocessor chỉ work với ảnh import LẦN ĐẦU. Ảnh đã có .meta trước đó
    /// vẫn giữ setting cũ (Multiple). Tool này fix các ảnh đó.
    ///
    /// Sử dụng:
    ///   1. Chọn folder hoặc nhiều ảnh trong Project window
    ///   2. Menu: GameTemplate → Asset Tools → Convert Selected Sprites to Single
    ///   3. Tool sẽ scan tất cả texture trong selection và đổi sang Single
    ///
    /// Hoặc convert toàn bộ project:
    ///   - Menu: GameTemplate → Asset Tools → Convert All Sprites to Single (Project)
    ///   - Cẩn thận: KHÔNG dùng nếu có sprite sheet animation Multiple thực sự.
    /// </summary>
    public static class SpriteModeBatchTool
    {
        [MenuItem("GameTemplate/Asset Tools/Convert Selected Sprites to Single", false, 100)]
        public static void ConvertSelectedToSingle()
        {
            var selected = Selection.assetGUIDs;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection",
                    "Chọn folder hoặc ảnh trong Project window trước.", "OK");
                return;
            }

            int converted = 0;
            int skipped = 0;

            try
            {
                AssetDatabase.StartAssetEditing(); // batch để nhanh

                foreach (var guid in selected)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    // Nếu là folder → scan tất cả texture bên trong
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        var texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
                        foreach (var texGuid in texGuids)
                        {
                            string texPath = AssetDatabase.GUIDToAssetPath(texGuid);
                            if (ConvertOne(texPath)) converted++;
                            else skipped++;
                        }
                    }
                    else if (ConvertOne(path))
                    {
                        converted++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("Done",
                $"Converted: {converted} sprites\nSkipped: {skipped} (already Single or not sprite)",
                "OK");
        }

        [MenuItem("GameTemplate/Asset Tools/Convert All Sprites to Single (Project)", false, 101)]
        public static void ConvertAllInProjectToSingle()
        {
            if (!EditorUtility.DisplayDialog("Warning",
                "Sẽ convert TẤT CẢ sprite trong Assets/_Project/ về Single mode.\n\n" +
                "⚠️ CẨN THẬN: nếu có sprite sheet animation Multiple thực sự, " +
                "chúng sẽ bị mất setup slicing!\n\n" +
                "Tiếp tục?", "Convert All", "Cancel"))
            {
                return;
            }

            var texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project" });
            int converted = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < texGuids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(texGuids[i]);
                    EditorUtility.DisplayProgressBar(
                        "Converting Sprites",
                        $"{i + 1}/{texGuids.Length}: {path}",
                        (float)i / texGuids.Length);

                    if (ConvertOne(path)) converted++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("Done",
                $"Converted {converted} / {texGuids.Length} sprites to Single mode.",
                "OK");
        }

        /// <summary>
        /// Convert 1 ảnh sang Single mode. Return true nếu thay đổi gì đó.
        /// Skip nếu: không phải Texture, không phải Sprite type, đã là Single.
        /// </summary>
        private static bool ConvertOne(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return false;

            // Chỉ áp dụng cho Sprite type
            if (importer.textureType != TextureImporterType.Sprite) return false;

            // Đã là Single rồi
            if (importer.spriteImportMode == SpriteImportMode.Single) return false;

            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
            return true;
        }

        // ============================================================
        // CHECK CURRENT STATE - liệt kê sprite đang Multiple
        // ============================================================
        [MenuItem("GameTemplate/Asset Tools/Find All Multiple-Mode Sprites", false, 102)]
        public static void FindAllMultipleSprites()
        {
            var texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project" });
            var multipleSprites = new System.Collections.Generic.List<string>();

            foreach (var guid in texGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                if (importer.textureType != TextureImporterType.Sprite) continue;
                if (importer.spriteImportMode == SpriteImportMode.Multiple)
                {
                    multipleSprites.Add(path);
                }
            }

            if (multipleSprites.Count == 0)
            {
                EditorUtility.DisplayDialog("Done",
                    "Không tìm thấy sprite nào đang ở Multiple mode.\nProject sạch! ✅",
                    "OK");
                return;
            }

            Debug.Log($"[Sprite Audit] Found {multipleSprites.Count} sprites in Multiple mode:");
            foreach (var path in multipleSprites)
            {
                Debug.Log($"  - {path}", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            }

            EditorUtility.DisplayDialog("Found",
                $"Tìm thấy {multipleSprites.Count} sprite đang Multiple mode.\n" +
                $"Xem chi tiết trong Console (click vào log để select asset).",
                "OK");
        }
    }
}
