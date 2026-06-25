using UnityEngine;
using GameTemplate.Core.Logger;

namespace GameTemplate.Core.Mobile.Device
{
    public enum DeviceTier { Low, Mid, High }

    /// <summary>
    /// Device Info Service - detect tier (Low/Mid/High) để adaptive quality.
    /// Mục tiêu: game chạy 60fps trên flagship, 30fps ổn định trên low-end.
    ///
    /// Tự động set:
    ///   - Quality level (URP asset hoặc QualitySettings)
    ///   - Target frame rate
    ///   - Texture quality
    ///   - Effect density (particle, shadow)
    ///
    /// User vẫn có thể override trong Settings menu.
    /// </summary>
    public interface IDeviceInfoService
    {
        DeviceTier Tier { get; }
        int SystemMemoryMb { get; }
        int GraphicsMemoryMb { get; }
        string DeviceModel { get; }
        string OsVersion { get; }
        bool IsLowEndDevice { get; }

        /// <summary>Áp dụng quality settings phù hợp tier hiện tại.</summary>
        void ApplyTierSettings();
    }

    public class DeviceInfoService : IDeviceInfoService
    {
        public DeviceTier Tier { get; private set; }
        public int SystemMemoryMb => SystemInfo.systemMemorySize;
        public int GraphicsMemoryMb => SystemInfo.graphicsMemorySize;
        public string DeviceModel => SystemInfo.deviceModel;
        public string OsVersion => SystemInfo.operatingSystem;
        public bool IsLowEndDevice => Tier == DeviceTier.Low;

        public DeviceInfoService()
        {
            Tier = DetectTier();
            GameLog.Info(LogCategory.Bootstrap,
                $"Device: {DeviceModel} | RAM: {SystemMemoryMb}MB | VRAM: {GraphicsMemoryMb}MB | Tier: {Tier}");
        }

        private DeviceTier DetectTier()
        {
            // Heuristic dựa trên RAM, GPU memory, processor count.
            // Có thể tinh chỉnh thêm bằng GPU name lookup (Adreno 6xx vs 5xx vs 4xx).
            int ram = SystemMemoryMb;
            int vram = GraphicsMemoryMb;
            int cores = SystemInfo.processorCount;

#if UNITY_IOS
            // iOS device tương đối đồng nhất - dùng generation số chip
            // iPhone 12+ (A14+): High
            // iPhone 8-11 (A11-A13): Mid
            // iPhone 7 trở xuống: Low
            string model = SystemInfo.deviceModel;
            if (model.Contains("iPhone1") && (model.Contains("iPhone13") || model.Contains("iPhone14") || model.Contains("iPhone15")))
                return DeviceTier.High;
            if (model.Contains("iPhone1") || model.Contains("iPhone9") || model.Contains("iPhone10"))
                return DeviceTier.Mid;
            if (ram >= 4000 && vram >= 1500) return DeviceTier.High;
            if (ram >= 2000 && vram >= 1000) return DeviceTier.Mid;
            return DeviceTier.Low;
#else
            // Android: dùng RAM + VRAM làm heuristic chính
            if (ram >= 6000 && vram >= 2000 && cores >= 8) return DeviceTier.High;
            if (ram >= 3000 && vram >= 1000 && cores >= 4) return DeviceTier.Mid;
            return DeviceTier.Low;
#endif
        }

        public void ApplyTierSettings()
        {
            switch (Tier)
            {
                case DeviceTier.Low:
                    Application.targetFrameRate = 30;
                    QualitySettings.SetQualityLevel(0, applyExpensiveChanges: true);
                    QualitySettings.globalTextureMipmapLimit = 1; // halve texture res
                    QualitySettings.shadows = ShadowQuality.Disable;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.realtimeReflectionProbes = false;
                    QualitySettings.softParticles = false;
                    QualitySettings.particleRaycastBudget = 16;
                    QualitySettings.skinWeights = SkinWeights.TwoBones;
                    QualitySettings.vSyncCount = 0;
                    break;

                case DeviceTier.Mid:
                    Application.targetFrameRate = 60;
                    QualitySettings.SetQualityLevel(1, applyExpensiveChanges: true);
                    QualitySettings.globalTextureMipmapLimit = 0;
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.realtimeReflectionProbes = false;
                    QualitySettings.softParticles = false;
                    QualitySettings.particleRaycastBudget = 64;
                    QualitySettings.skinWeights = SkinWeights.FourBones;
                    QualitySettings.vSyncCount = 0;
                    break;

                case DeviceTier.High:
                    Application.targetFrameRate = 60;
                    QualitySettings.SetQualityLevel(2, applyExpensiveChanges: true);
                    QualitySettings.globalTextureMipmapLimit = 0;
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    QualitySettings.antiAliasing = 2; // 2x MSAA
                    QualitySettings.realtimeReflectionProbes = true;
                    QualitySettings.softParticles = true;
                    QualitySettings.particleRaycastBudget = 256;
                    QualitySettings.skinWeights = SkinWeights.FourBones;
                    QualitySettings.vSyncCount = 0;
                    break;
            }
            GameLog.Info(LogCategory.Bootstrap, $"Applied quality settings for tier: {Tier}");
        }
    }
}
