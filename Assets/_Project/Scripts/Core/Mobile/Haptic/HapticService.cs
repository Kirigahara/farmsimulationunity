using UnityEngine;
using GameTemplate.Core.Logger;

namespace GameTemplate.Core.Mobile.Haptic
{
    public enum HapticType
    {
        None,      // tắt haptic (cho component có toggle on/off)
        Light,     // tap UI nhẹ
        Medium,    // collect coin, hit enemy
        Heavy,     // explosion, big impact
        Success,   // hoàn thành level
        Warning,   // sắp thua, HP thấp
        Failure,   // game over
        Selection  // chọn menu item
    }

    /// <summary>
    /// Haptic Service - rung mobile.
    /// iOS: dùng Taptic Engine (sharp, có nhiều level - tốt nhất)
    /// Android: dùng Vibrator API (chỉ rung đơn giản, có level từ API 26+)
    /// Editor: log để debug
    ///
    /// User experience:
    ///   - Hyper-casual: dùng Light cho tap, Medium cho collect, Heavy cho crash
    ///   - Puzzle: dùng Selection cho move, Success khi solve
    ///   - RPG: Medium cho hit, Heavy cho ulti, Warning khi HP thấp
    ///
    /// Phải có setting "Tắt rung" trong game (giữ trong PlayerPrefs).
    /// </summary>
    public interface IHapticService
    {
        bool IsEnabled { get; set; }
        bool IsSupported { get; }
        void Play(HapticType type);
    }

    public class HapticService : IHapticService
    {
        private const string PrefsKey = "haptic_enabled";

        public bool IsEnabled
        {
            get => PlayerPrefs.GetInt(PrefsKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(PrefsKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public bool IsSupported
        {
            get
            {
#if UNITY_IOS || UNITY_ANDROID
                return SystemInfo.supportsVibration;
#else
                return false;
#endif
            }
        }

        public void Play(HapticType type)
        {
            if (type == HapticType.None) return;
            if (!IsEnabled) return;
            if (!IsSupported)
            {
                GameLog.Info(LogCategory.UI, $"[Haptic] Mock: {type}");
                return;
            }

#if UNITY_IOS
            PlayIOS(type);
#elif UNITY_ANDROID
            PlayAndroid(type);
#endif
        }

#if UNITY_IOS
        // iOS Taptic Engine - dùng UIImpactFeedbackGenerator qua Objective-C bridge.
        // RECOMMEND: import package "Lofelt Nice Vibrations" hoặc tự viết native plugin.
        // Đây là fallback đơn giản dùng Handheld.Vibrate (mạnh nhưng không phân level).
        private void PlayIOS(HapticType type)
        {
            // TODO: native plugin Taptic Engine cho UX tốt hơn.
            // Hiện tại fallback Handheld.Vibrate (giống Android).
            Handheld.Vibrate();
        }
#endif

#if UNITY_ANDROID
        // Android API 26+: dùng VibrationEffect để có pattern và amplitude.
        // API < 26: chỉ Vibrate(milliseconds) đơn giản.
        private void PlayAndroid(HapticType type)
        {
#if UNITY_2020_1_OR_NEWER
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                long duration = GetDurationMs(type);
                int amplitude = GetAmplitude(type);

                // Check API level 26+ cho VibrationEffect
                if (AndroidVersion >= 26)
                {
                    using (var vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                        "createOneShot", duration, amplitude))
                    {
                        vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    vibrator.Call("vibrate", duration);
                }
            }
#else
            Handheld.Vibrate();
#endif
        }

        private static int AndroidVersion
        {
            get
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    return version.GetStatic<int>("SDK_INT");
            }
        }

        private long GetDurationMs(HapticType type)
        {
            switch (type)
            {
                case HapticType.Light: return 10;
                case HapticType.Medium: return 25;
                case HapticType.Heavy: return 50;
                case HapticType.Success: return 30;
                case HapticType.Warning: return 100;
                case HapticType.Failure: return 200;
                case HapticType.Selection: return 8;
                default: return 20;
            }
        }

        private int GetAmplitude(HapticType type)
        {
            // 1-255, hoặc -1 để dùng default
            switch (type)
            {
                case HapticType.Light: return 60;
                case HapticType.Medium: return 130;
                case HapticType.Heavy: return 255;
                case HapticType.Success: return 150;
                case HapticType.Warning: return 200;
                case HapticType.Failure: return 255;
                case HapticType.Selection: return 50;
                default: return 100;
            }
        }
#endif
    }
}
