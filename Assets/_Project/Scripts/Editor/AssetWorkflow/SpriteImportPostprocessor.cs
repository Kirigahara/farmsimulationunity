using UnityEditor;
using UnityEngine;

namespace GameTemplate.Editor.AssetWorkflow
{
    /// <summary>
    /// Auto-config sprite mode khi import texture 2D.
    ///
    /// Vấn đề: Unity 6.0 default Sprite Mode = Multiple → mỗi lần kéo ảnh vào dev
    /// phải đổi thủ công về Single. Annoying khi import 100+ icon.
    ///
    /// Giải pháp: AssetPostprocessor hook vào lifecycle import:
    ///   - OnPreprocessTexture: chạy TRƯỚC khi Unity import → set config
    ///   - Unity import theo config đã set → không cần đổi tay
    ///
    /// Convention folder:
    ///   - Assets/_Project/Art/UI/        → Single (icon, button, panel)
    ///   - Assets/_Project/Art/Sprites/   → Single (gameplay sprite đơn)
    ///   - Assets/_Project/Art/SpriteSheets/ → Multiple (animation sprite sheet)
    ///   - Assets/_Project/Art/Backgrounds/ → Single, không generate mipmap
    ///   - Mặc định: Single
    ///
    /// CHỈ áp dụng cho ảnh import LẦN ĐẦU - không động vào ảnh đã import (giữ setting cũ).
    /// Nếu muốn re-apply: chọn ảnh → right-click → Reimport.
    /// </summary>
    public class SpriteImportPostprocessor : AssetPostprocessor
    {
        // ============================================================
        // CONFIG - sửa các path này theo cấu trúc project của bạn
        // ============================================================

        // Path chứa sprite sheet animation - default Multiple
        private static readonly string[] _multipleSpriteFolders = new[]
        {
            "/Art/SpriteSheets/",
            "/Art/Animations/",
            "/Art/Atlases/",
        };

        // Path chứa background lớn - tắt mipmap để tiết kiệm memory
        private static readonly string[] _backgroundFolders = new[]
        {
            "/Art/Backgrounds/",
            "/Art/Splash/",
        };

        // ============================================================
        // PREPROCESS - chạy TRƯỚC Unity import
        // ============================================================
        private void OnPreprocessTexture()
        {
            var importer = (TextureImporter)assetImporter;

            // CHỈ áp dụng cho ảnh import LẦN ĐẦU
            // Nếu đã import rồi (có .meta), giữ setting cũ - không override.
            // Để re-apply: user phải right-click → Reimport thủ công.
            if (!IsFirstImport()) return;

            // Bỏ qua nếu file không nằm trong project art folder (vd: third-party plugin)
            if (!assetPath.Contains("/_Project/")) return;

            // ===== Cấu hình chung =====
            importer.textureType = TextureImporterType.Sprite;

            // ===== Sprite mode - theo folder =====
            if (IsInAnyFolder(_multipleSpriteFolders))
            {
                // Sprite sheet animation
                importer.spriteImportMode = SpriteImportMode.Multiple;
            }
            else
            {
                // Default: Single (icon, button, background, gameplay sprite)
                importer.spriteImportMode = SpriteImportMode.Single;
            }

            // ===== Pixel per unit - đồng nhất 100 PPU =====
            importer.spritePixelsPerUnit = 100f;

            // ===== Filter mode - Bilinear cho UI mượt =====
            importer.filterMode = FilterMode.Bilinear;

            // ===== Mipmap - tắt cho UI, bật cho world sprite =====
            // UI sprite không cần mipmap (luôn 1:1 pixel), tiết kiệm 33% memory
            bool isUI = assetPath.Contains("/Art/UI/");
            importer.mipmapEnabled = !isUI;

            // Background lớn cũng tắt mipmap
            if (IsInAnyFolder(_backgroundFolders))
            {
                importer.mipmapEnabled = false;
            }

            // ===== Compression - tự chọn theo platform =====
            // Default compression đã ổn, dev có thể tinh chỉnh từng ảnh sau

            // ===== Max size - hạn chế ảnh quá to =====
            // Mobile thường không cần > 2048
            importer.maxTextureSize = 2048;

            Debug.Log(
                $"[SpriteImport] Auto-config: {assetPath}\n" +
                $"  Mode: {importer.spriteImportMode}, Mipmap: {importer.mipmapEnabled}, " +
                $"PPU: {importer.spritePixelsPerUnit}");
        }

        // ============================================================
        // HELPERS
        // ============================================================
        private bool IsFirstImport()
        {
            // Unity gọi OnPreprocessTexture cả khi reimport.
            // Check first import: file .meta chưa tồn tại trước khi import.
            // AssetImporter.importSettingsMissing = true khi lần đầu.
            return assetImporter.importSettingsMissing;
        }

        private bool IsInAnyFolder(string[] folders)
        {
            foreach (var folder in folders)
            {
                if (assetPath.Contains(folder)) return true;
            }
            return false;
        }
    }
}
