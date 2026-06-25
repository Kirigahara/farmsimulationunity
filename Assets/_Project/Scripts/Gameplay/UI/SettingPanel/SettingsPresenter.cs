using GameTemplate.Core.DI;
using GameTemplate.Core.Mobile.Haptic;
using GameTemplate.Core.Mobile.Localization;
using GameTemplate.Core.Patterns.MVP;

namespace GameTemplate.Gameplay.UI.SettingsPanel
{
    /// <summary>
    /// SettingsPresenter - cầu nối Model và View.
    /// Pure C# class, không kế thừa MonoBehaviour -> test bằng NUnit không cần Unity Editor.
    ///
    /// Flow:
    ///   - View event (user tương tác) -> Presenter call Model.SetXxx() -> Model update Service + ReactiveProperty
    ///   - Reactive subscribe (Presenter subscribe Model) -> Model.XxxChanged -> Presenter call View.SetXxx()
    /// </summary>
    public class SettingsPresenter : PresenterBase<SettingsView, SettingsModel>
    {
        public SettingsPresenter(SettingsView view, SettingsModel model) : base(view, model) { }

        protected override void OnInit()
        {
            // ===== View events -> Model methods =====
            View.OnMusicVolumeChanged += OnMusicVolumeChanged;
            View.OnSfxVolumeChanged += OnSfxVolumeChanged;
            View.OnMusicMuteClicked += OnMusicMuteClicked;
            View.OnSfxMuteClicked += OnSfxMuteClicked;
            View.OnHapticToggled += OnHapticToggled;
            View.OnLanguageSelected += OnLanguageSelected;

            // ===== Model reactive -> View update =====
            // SubscribeWithInit để vừa subscribe vừa init UI với value hiện tại
            Model.MusicVolume.SubscribeWithInit(View.SetMusicVolume);
            Model.SfxVolume.SubscribeWithInit(View.SetSfxVolume);
            Model.MusicMuted.SubscribeWithInit(View.SetMusicMuteIcon);
            Model.SfxMuted.SubscribeWithInit(View.SetSfxMuteIcon);
            Model.HapticEnabled.SubscribeWithInit(View.SetHapticToggle);

            // Language cần data nhiều hơn (available list)
            var loc = ServiceLocator.Get<ILocalizationService>();
            View.SetLanguageDropdown(loc.AvailableLanguages, Model.Language.Value);
            Model.Language.Subscribe(OnLanguageChangedRefreshUI);

            // Init labels lần đầu
            RefreshLabels();
        }

        protected override void OnDispose()
        {
            View.OnMusicVolumeChanged -= OnMusicVolumeChanged;
            View.OnSfxVolumeChanged -= OnSfxVolumeChanged;
            View.OnMusicMuteClicked -= OnMusicMuteClicked;
            View.OnSfxMuteClicked -= OnSfxMuteClicked;
            View.OnHapticToggled -= OnHapticToggled;
            View.OnLanguageSelected -= OnLanguageSelected;

            Model.MusicVolume.Unsubscribe(View.SetMusicVolume);
            Model.SfxVolume.Unsubscribe(View.SetSfxVolume);
            Model.MusicMuted.Unsubscribe(View.SetMusicMuteIcon);
            Model.SfxMuted.Unsubscribe(View.SetSfxMuteIcon);
            Model.HapticEnabled.Unsubscribe(View.SetHapticToggle);
            Model.Language.Unsubscribe(OnLanguageChangedRefreshUI);
        }

        // ===== View events handlers =====

        private void OnMusicVolumeChanged(float value)
        {
            Model.SetMusicVolume(value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            Model.SetSfxVolume(value);
            // Phát test SFX khi user chỉnh slider để nghe ngay volume mới
            var clip = View.GetTestSfxClip();
            if (clip != null) Model.PlayTestSfx(clip);
        }

        private void OnMusicMuteClicked()
        {
            Model.ToggleMusicMute();
            // Haptic feedback khi tap button - dùng service trực tiếp cũng OK
            ServiceLocator.Get<IHapticService>().Play(HapticType.Selection);
        }

        private void OnSfxMuteClicked()
        {
            Model.ToggleSfxMute();
            ServiceLocator.Get<IHapticService>().Play(HapticType.Selection);
        }

        private void OnHapticToggled(bool on)
        {
            Model.SetHapticEnabled(on);
            // Nếu vừa bật haptic, rung 1 cái để user xác nhận thấy
            if (on) ServiceLocator.Get<IHapticService>().Play(HapticType.Success);
        }

        private void OnLanguageSelected(GameLanguage lang)
        {
            Model.SetLanguage(lang);
            // Model sẽ trigger Language.Value đổi -> OnLanguageChangedRefreshUI tự gọi
        }

        // ===== Model reactive handlers =====

        private void OnLanguageChangedRefreshUI(GameLanguage lang)
        {
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            var loc = ServiceLocator.Get<ILocalizationService>();
            View.RefreshLabels(
                title: loc.Get("settings.title", "Settings"),
                music: loc.Get("settings.music", "Music"),
                sfx: loc.Get("settings.sfx", "Sound Effects"),
                haptic: loc.Get("settings.haptic", "Haptic Feedback"),
                language: loc.Get("settings.language", "Language")
            );
        }
    }
}
