using GameTemplate.Core.DI;
using GameTemplate.Core.Audio;
using GameTemplate.Core.Mobile.Haptic;
using GameTemplate.Core.Mobile.Localization;
using GameTemplate.Core.Patterns.Reactive;

namespace GameTemplate.Gameplay.UI.SettingsPanel
{
    /// <summary>
    /// SettingsModel - chứa data + business logic của settings.
    /// Pure C# class, KHÔNG kế thừa MonoBehaviour, KHÔNG biết Unity UI.
    ///
    /// Mỗi setting là ReactiveProperty -> UI subscribe tự update khi value đổi.
    /// Model lo bridge với underlying services (AudioService, HapticService, Localization).
    /// </summary>
    public class SettingsModel
    {
        // Inject services qua constructor
        private readonly IAudioService _audio;
        private readonly IHapticService _haptic;
        private readonly ILocalizationService _localization;

        // ===== Reactive state - UI subscribe vào đây =====
        public ReactiveProperty<float> MusicVolume { get; }
        public ReactiveProperty<float> SfxVolume { get; }
        public ReactiveProperty<bool> MusicMuted { get; }
        public ReactiveProperty<bool> SfxMuted { get; }
        public ReactiveProperty<bool> HapticEnabled { get; }
        public ReactiveProperty<GameLanguage> Language { get; }

        public SettingsModel(
            IAudioService audio,
            IHapticService haptic,
            ILocalizationService localization)
        {
            _audio = audio;
            _haptic = haptic;
            _localization = localization;

            // Init reactive properties từ giá trị hiện tại của service
            // SetSilent để không trigger callback khi init (tránh re-set service)
            MusicVolume = new ReactiveProperty<float>(_audio.MusicVolume);
            SfxVolume = new ReactiveProperty<float>(_audio.SfxVolume);
            MusicMuted = new ReactiveProperty<bool>(_audio.IsMusicMuted);
            SfxMuted = new ReactiveProperty<bool>(_audio.IsSfxMuted);
            HapticEnabled = new ReactiveProperty<bool>(_haptic.IsEnabled);
            Language = new ReactiveProperty<GameLanguage>(_localization.CurrentLanguage);

            // Wire service event -> reactive property
            // Cần đồng bộ 2 chiều: nếu service đổi từ chỗ khác, Model cũng update
            _audio.OnAudioSettingsChanged += SyncFromAudioService;
            _localization.OnLanguageChanged += SyncFromLocalization;
        }

        // ===== Public API: gọi từ Presenter khi user tương tác UI =====

        public void SetMusicVolume(float value)
        {
            _audio.MusicVolume = value;
            MusicVolume.Value = value; // notify UI
        }

        public void SetSfxVolume(float value)
        {
            _audio.SfxVolume = value;
            SfxVolume.Value = value;
        }

        public void ToggleMusicMute()
        {
            var newState = _audio.ToggleMusic();
            MusicMuted.Value = newState;
        }

        public void ToggleSfxMute()
        {
            var newState = _audio.ToggleSfx();
            SfxMuted.Value = newState;
        }

        public void SetHapticEnabled(bool enabled)
        {
            _haptic.IsEnabled = enabled;
            HapticEnabled.Value = enabled;
        }

        public void SetLanguage(GameLanguage lang)
        {
            _localization.SetLanguage(lang);
            Language.Value = lang;
        }

        /// <summary>Test SFX cho user nghe khi chỉnh volume.</summary>
        public void PlayTestSfx(UnityEngine.AudioClip clip)
        {
            _audio.PlaySfx(clip);
        }

        // ===== Sync từ service về Model (khi service đổi từ chỗ khác) =====

        private void SyncFromAudioService()
        {
            // Dùng SetSilent để tránh loop: UI -> Model -> Service -> SyncBack -> UI
            // (vì service đã apply rồi, UI đã update rồi)
            MusicVolume.SetSilent(_audio.MusicVolume);
            SfxVolume.SetSilent(_audio.SfxVolume);
            MusicMuted.SetSilent(_audio.IsMusicMuted);
            SfxMuted.SetSilent(_audio.IsSfxMuted);

            // Force notify để UI refresh (cần thiết khi service đổi từ chỗ khác, không phải qua Model)
            MusicVolume.ForceNotify();
            SfxVolume.ForceNotify();
            MusicMuted.ForceNotify();
            SfxMuted.ForceNotify();
        }

        private void SyncFromLocalization()
        {
            Language.Value = _localization.CurrentLanguage;
        }

        /// <summary>Cleanup khi Model không còn dùng nữa.</summary>
        public void Dispose()
        {
            _audio.OnAudioSettingsChanged -= SyncFromAudioService;
            _localization.OnLanguageChanged -= SyncFromLocalization;
            MusicVolume.ClearSubscribers();
            SfxVolume.ClearSubscribers();
            MusicMuted.ClearSubscribers();
            SfxMuted.ClearSubscribers();
            HapticEnabled.ClearSubscribers();
            Language.ClearSubscribers();
        }
    }
}
