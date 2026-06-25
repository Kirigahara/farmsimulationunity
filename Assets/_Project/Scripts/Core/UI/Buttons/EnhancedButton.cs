using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GameTemplate.Core.Audio;
using GameTemplate.Core.DI;
using GameTemplate.Core.Logger;
using GameTemplate.Core.Mobile.Analytics;
using GameTemplate.Core.Mobile.Haptic;

namespace GameTemplate.Core.UI.Buttons
{
    /// <summary>
    /// Preset SFX cho button - map sang AudioClip trong UIButtonSfxLibrary asset.
    /// </summary>
    public enum ButtonSfxPreset
    {
        None,      // Không phát SFX
        Click,     // SFX click thông thường
        Confirm,   // OK, Submit, Buy
        Cancel,    // Close, Back, Dismiss
        Error,     // Disabled, fail
        Custom,    // Dùng AudioClip kéo ở field _customSfx
    }

    /// <summary>
    /// Button mở rộng của Unity - giữ nguyên mọi feature gốc (transition, navigation, OnClick),
    /// thêm:
    ///   - SFX: preset (Click/Confirm/Cancel/Error) hoặc Custom clip
    ///   - Analytics: TrackEvent(name) - không param
    ///   - Haptic: rung khi click
    ///   - Anti-spam: chống user click double quá nhanh
    /// </summary>
    [AddComponentMenu("UI/Enhanced Button")]
    public class EnhancedButton : Button
    {
        [Header("Sound Effect")]
        [SerializeField] private ButtonSfxPreset _sfxPreset = ButtonSfxPreset.Click;
        [Tooltip("Override clip - dùng khi SfxPreset = Custom, hoặc khi muốn replace preset.")]
        [SerializeField] private AudioClip _customSfx;
        [Range(0f, 1f)]
        [SerializeField] private float _sfxVolumeScale = 1f;

        [Header("Analytics Tracking")]
        [Tooltip("Tên event Analytics. Để trống = không track.")]
        [SerializeField] private string _trackEventName = "";

        [Header("Haptic Feedback (Mobile)")]
        [SerializeField] private HapticType _hapticType = HapticType.Selection;

        [Header("Spam Protection")]
        [Tooltip("Thời gian tối thiểu giữa 2 click (giây). 0 = tắt protection.")]
        [SerializeField] private float _minIntervalBetweenClicks = 0.2f;

        private float _lastClickTime = -10f;

        // ============================================================
        public override void OnPointerClick(PointerEventData eventData)
        {
            // 1. Spam protection
            if (_minIntervalBetweenClicks > 0
                && Time.unscaledTime - _lastClickTime < _minIntervalBetweenClicks)
            {
                return;
            }
            _lastClickTime = Time.unscaledTime;

            // 2. Play SFX (làm trước OnClick để feel responsive)
            PlaySfx();

            // 3. Haptic feedback (mobile)
            PlayHaptic();

            // 4. Track event (không block click)
            TrackClick();

            // 5. Gọi Button.OnPointerClick gốc → fire onClick listener
            base.OnPointerClick(eventData);
        }

        // ============================================================
        // SFX - ưu tiên _customSfx nếu có, không thì fallback preset library
        // ============================================================
        private void PlaySfx()
        {
            if (_sfxPreset == ButtonSfxPreset.None) return;
            if (!ServiceLocator.TryGet<IAudioService>(out var audio)) return;

            // Nếu có custom clip → dùng (override preset)
            // → cho phép designer cùng dùng preset Click nhưng override 1 nút riêng
            if (_customSfx != null)
            {
                audio.PlaySfx(_customSfx, _sfxVolumeScale);
                return;
            }

            // Lookup library cho preset
            if (_sfxPreset == ButtonSfxPreset.Custom)
            {
                // Custom nhưng không kéo clip → bỏ qua silent
                return;
            }

            if (!ServiceLocator.TryGet<IUIButtonSfxLibrary>(out var library))
            {
                GameLog.Warning(LogCategory.UI,
                    $"[EnhancedButton] Không tìm thấy UIButtonSfxLibrary - preset '{_sfxPreset}' sẽ không phát. " +
                    "Tạo asset và kéo vào MobileServicesBootstrapper.");
                return;
            }

            var clip = library.GetClip(_sfxPreset);
            if (clip != null) audio.PlaySfx(clip, _sfxVolumeScale);
        }

        // ============================================================
        // HAPTIC
        // ============================================================
        private void PlayHaptic()
        {
            if (_hapticType == HapticType.None) return;
            if (ServiceLocator.TryGet<IHapticService>(out var haptic))
                haptic.Play(_hapticType);
        }

        // ============================================================
        // TRACKING - chỉ event name, không param
        // ============================================================
        private void TrackClick()
        {
            if (string.IsNullOrEmpty(_trackEventName)) return;
            if (ServiceLocator.TryGet<IAnalyticsService>(out var analytics))
                analytics.TrackEvent(_trackEventName);
        }

        // ============================================================
        // PUBLIC API - cho code đổi runtime
        // ============================================================
        public void SetTrackEvent(string eventName) => _trackEventName = eventName;
    }
}
