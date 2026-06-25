using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameTemplate.Core.Mobile.Localization;
using GameTemplate.Core.Patterns.MVP;

namespace GameTemplate.Gameplay.UI.SettingsPanel
{
    /// <summary>
    /// SettingsView - MonoBehaviour, chỉ làm việc với Unity UI component.
    /// KHÔNG chứa business logic, KHÔNG biết Model.
    ///
    /// Presenter inject vào View và gọi method (SetMusicVolume, SetMuteIcon...).
    /// View fire C# event khi user tương tác - Presenter subscribe.
    ///
    /// Lợi ích: thay UI hoàn toàn (vd dùng TextMeshPro thay Text, hoặc custom button)
    /// chỉ cần đổi View, Presenter và Model không sửa.
    /// </summary>
    public class SettingsView : ViewBase
    {
        [Header("Music")]
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Button _musicMuteButton;
        [SerializeField] private Image _musicMuteIcon;

        [Header("SFX")]
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Button _sfxMuteButton;
        [SerializeField] private Image _sfxMuteIcon;
        [SerializeField] private AudioClip _testSfxClip;

        [Header("Haptic")]
        [SerializeField] private Toggle _hapticToggle;

        [Header("Language")]
        [SerializeField] private Dropdown _languageDropdown;

        [Header("Icons")]
        [SerializeField] private Sprite _iconSoundOn;
        [SerializeField] private Sprite _iconSoundOff;

        [Header("Labels (cho localization)")]
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _musicLabel;
        [SerializeField] private Text _sfxLabel;
        [SerializeField] private Text _hapticLabel;
        [SerializeField] private Text _languageLabel;

        // Sẽ map index dropdown -> GameLanguage enum
        private List<GameLanguage> _availableLanguages;

        // ===== Events - Presenter subscribe =====
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSfxVolumeChanged;
        public event Action OnMusicMuteClicked;
        public event Action OnSfxMuteClicked;
        public event Action<bool> OnHapticToggled;
        public event Action<GameLanguage> OnLanguageSelected;
        public event Action OnTestSfxRequested; // khi user thả slider SFX -> phát test sound

        // ===== Lifecycle =====
        public override void Bind()
        {
            // Wire UI -> events
            _musicVolumeSlider.onValueChanged.AddListener(v => OnMusicVolumeChanged?.Invoke(v));
            _sfxVolumeSlider.onValueChanged.AddListener(v => OnSfxVolumeChanged?.Invoke(v));
            _musicMuteButton.onClick.AddListener(() => OnMusicMuteClicked?.Invoke());
            _sfxMuteButton.onClick.AddListener(() => OnSfxMuteClicked?.Invoke());
            _hapticToggle.onValueChanged.AddListener(on => OnHapticToggled?.Invoke(on));
            _languageDropdown.onValueChanged.AddListener(OnDropdownChanged);

            // Khi user thả slider SFX, play test clip để nghe volume mới
            // Dùng EventTrigger hoặc trick: listen OnEndDrag
            // Đơn giản: chỉ phát khi user nhả slider (onValueChanged fire liên tục khi drag)
            // -> wire qua EventTrigger trong Inspector hoặc dùng AddListener khác
        }

        public override void Unbind()
        {
            _musicVolumeSlider.onValueChanged.RemoveAllListeners();
            _sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            _musicMuteButton.onClick.RemoveAllListeners();
            _sfxMuteButton.onClick.RemoveAllListeners();
            _hapticToggle.onValueChanged.RemoveAllListeners();
            _languageDropdown.onValueChanged.RemoveAllListeners();
        }

        // ===== API cho Presenter gọi =====

        public void SetMusicVolume(float value)
        {
            // SetValueWithoutNotify để khỏi fire OnMusicVolumeChanged khi Presenter set
            // (tránh loop UI -> Model -> Service -> Presenter -> UI -> ...)
            _musicVolumeSlider.SetValueWithoutNotify(value);
        }

        public void SetSfxVolume(float value)
        {
            _sfxVolumeSlider.SetValueWithoutNotify(value);
        }

        public void SetMusicMuteIcon(bool muted)
        {
            _musicMuteIcon.sprite = muted ? _iconSoundOff : _iconSoundOn;
            // Disable slider khi mute để UI nhất quán
            _musicVolumeSlider.interactable = !muted;
        }

        public void SetSfxMuteIcon(bool muted)
        {
            _sfxMuteIcon.sprite = muted ? _iconSoundOff : _iconSoundOn;
            _sfxVolumeSlider.interactable = !muted;
        }

        public void SetHapticToggle(bool on)
        {
            _hapticToggle.SetIsOnWithoutNotify(on);
        }

        public void SetLanguageDropdown(IReadOnlyList<GameLanguage> available, GameLanguage current)
        {
            _availableLanguages = new List<GameLanguage>(available);
            _languageDropdown.ClearOptions();
            var options = new List<string>();
            foreach (var lang in _availableLanguages)
                options.Add(GetLanguageDisplayName(lang));
            _languageDropdown.AddOptions(options);

            int index = _availableLanguages.IndexOf(current);
            if (index >= 0) _languageDropdown.SetValueWithoutNotify(index);
        }

        public void RefreshLabels(string title, string music, string sfx, string haptic, string language)
        {
            // Khi đổi ngôn ngữ, Presenter gọi method này với text đã translate
            _titleLabel.text = title;
            _musicLabel.text = music;
            _sfxLabel.text = sfx;
            _hapticLabel.text = haptic;
            _languageLabel.text = language;
        }

        public AudioClip GetTestSfxClip() => _testSfxClip;

        // ===== Internal =====
        private void OnDropdownChanged(int index)
        {
            if (_availableLanguages == null || index < 0 || index >= _availableLanguages.Count) return;
            OnLanguageSelected?.Invoke(_availableLanguages[index]);
        }

        private static string GetLanguageDisplayName(GameLanguage lang)
        {
            // Mỗi ngôn ngữ hiển thị tên gốc của nó (user dễ nhận)
            switch (lang)
            {
                case GameLanguage.English: return "English";
                case GameLanguage.Vietnamese: return "Tiếng Việt";
                case GameLanguage.Japanese: return "日本語";
                case GameLanguage.Korean: return "한국어";
                case GameLanguage.ChineseSimplified: return "简体中文";
                case GameLanguage.ChineseTraditional: return "繁體中文";
                case GameLanguage.Spanish: return "Español";
                case GameLanguage.French: return "Français";
                case GameLanguage.German: return "Deutsch";
                case GameLanguage.Italian: return "Italiano";
                case GameLanguage.Portuguese: return "Português";
                case GameLanguage.Russian: return "Русский";
                case GameLanguage.Thai: return "ไทย";
                case GameLanguage.Indonesian: return "Bahasa Indonesia";
                default: return lang.ToString();
            }
        }
    }
}
