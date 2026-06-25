using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using GameTemplate.Core.Logger;

namespace GameTemplate.Core.Audio
{
    public interface IAudioService
    {
        // ========== Mute API (cho game không cần slider volume) ==========
        /// <summary>True = tắt toàn bộ âm thanh (music + sfx).</summary>
        bool IsMasterMuted { get; set; }
        /// <summary>True = tắt riêng nhạc nền.</summary>
        bool IsMusicMuted { get; set; }
        /// <summary>True = tắt riêng SFX.</summary>
        bool IsSfxMuted { get; set; }

        /// <summary>Toggle nhanh - return state mới sau khi toggle.</summary>
        bool ToggleMaster();
        bool ToggleMusic();
        bool ToggleSfx();

        // ========== Volume API (cho game có slider 0-1) ==========
        /// <summary>Volume 0..1 cho master. Set sẽ auto save PlayerPrefs.</summary>
        float MasterVolume { get; set; }
        float MusicVolume { get; set; }
        float SfxVolume { get; set; }

        // ========== Events - UI có thể subscribe để refresh icon ==========
        event Action OnAudioSettingsChanged;

        // ========== Playback ==========
        void PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f);
        void PlayMusic(AudioClip clip, bool loop = true, float fadeIn = 1f);
        void StopMusic(float fadeOut = 1f);
    }

    /// <summary>
    /// Audio Manager với SFX pool + Mute API + auto persist PlayerPrefs.
    ///
    /// Mute vs Volume:
    ///   - Volume 0..1: cho game có slider chi tiết
    ///   - Mute on/off: cho game chỉ cần icon 🔊/🔇 (hyper-casual, puzzle)
    ///   - 2 cái độc lập: mute không reset volume. Unmute lại đúng volume trước đó.
    ///
    /// PlayerPrefs keys:
    ///   - audio_master_muted, audio_music_muted, audio_sfx_muted (bool int 0/1)
    ///   - audio_master_volume, audio_music_volume, audio_sfx_volume (float 0..1)
    /// </summary>
    public class AudioManager : MonoBehaviour, IAudioService
    {
        [Header("Mixer Setup")]
        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private AudioMixerGroup _sfxGroup;
        [SerializeField] private AudioMixerGroup _musicGroup;

        [Header("Pool")]
        [SerializeField] private int _sfxPoolSize = 16;

        [Header("Mixer Parameters (expose trên mixer)")]
        [SerializeField] private string _masterParam = "MasterVolume";
        [SerializeField] private string _sfxParam = "SfxVolume";
        [SerializeField] private string _musicParam = "MusicVolume";

        [Header("Defaults (khi player chơi lần đầu)")]
        [Range(0f, 1f)][SerializeField] private float _defaultMasterVolume = 1f;
        [Range(0f, 1f)][SerializeField] private float _defaultMusicVolume = 0.7f;
        [Range(0f, 1f)][SerializeField] private float _defaultSfxVolume = 1f;

        // PlayerPrefs keys
        private const string KeyMasterMuted = "audio_master_muted";
        private const string KeyMusicMuted = "audio_music_muted";
        private const string KeySfxMuted = "audio_sfx_muted";
        private const string KeyMasterVolume = "audio_master_volume";
        private const string KeyMusicVolume = "audio_music_volume";
        private const string KeySfxVolume = "audio_sfx_volume";

        private readonly Queue<AudioSource> _sfxPool = new Queue<AudioSource>();
        private AudioSource _musicSource;

        // State (backing fields)
        private bool _masterMuted, _musicMuted, _sfxMuted;
        private float _masterVolume, _musicVolume, _sfxVolume;

        public event Action OnAudioSettingsChanged;

        // ============ Public properties ============

        public bool IsMasterMuted
        {
            get => _masterMuted;
            set
            {
                if (_masterMuted == value) return;
                _masterMuted = value;
                PlayerPrefs.SetInt(KeyMasterMuted, value ? 1 : 0);
                PlayerPrefs.Save();
                ApplyMasterVolume();
                OnAudioSettingsChanged?.Invoke();
                GameLog.Info(LogCategory.Audio, $"Master muted: {value}");
            }
        }

        public bool IsMusicMuted
        {
            get => _musicMuted;
            set
            {
                if (_musicMuted == value) return;
                _musicMuted = value;
                PlayerPrefs.SetInt(KeyMusicMuted, value ? 1 : 0);
                PlayerPrefs.Save();
                ApplyMusicVolume();
                OnAudioSettingsChanged?.Invoke();
                GameLog.Info(LogCategory.Audio, $"Music muted: {value}");
            }
        }

        public bool IsSfxMuted
        {
            get => _sfxMuted;
            set
            {
                if (_sfxMuted == value) return;
                _sfxMuted = value;
                PlayerPrefs.SetInt(KeySfxMuted, value ? 1 : 0);
                PlayerPrefs.Save();
                ApplySfxVolume();
                OnAudioSettingsChanged?.Invoke();
                GameLog.Info(LogCategory.Audio, $"SFX muted: {value}");
            }
        }

        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(_masterVolume, value)) return;
                _masterVolume = value;
                PlayerPrefs.SetFloat(KeyMasterVolume, value);
                PlayerPrefs.Save();
                ApplyMasterVolume();
                OnAudioSettingsChanged?.Invoke();
            }
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(_musicVolume, value)) return;
                _musicVolume = value;
                PlayerPrefs.SetFloat(KeyMusicVolume, value);
                PlayerPrefs.Save();
                ApplyMusicVolume();
                OnAudioSettingsChanged?.Invoke();
            }
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(_sfxVolume, value)) return;
                _sfxVolume = value;
                PlayerPrefs.SetFloat(KeySfxVolume, value);
                PlayerPrefs.Save();
                ApplySfxVolume();
                OnAudioSettingsChanged?.Invoke();
            }
        }

        // ============ Toggle helpers ============

        public bool ToggleMaster() { IsMasterMuted = !IsMasterMuted; return IsMasterMuted; }
        public bool ToggleMusic() { IsMusicMuted = !IsMusicMuted; return IsMusicMuted; }
        public bool ToggleSfx() { IsSfxMuted = !IsSfxMuted; return IsSfxMuted; }

        // ============ Awake: setup pool + load saved settings ============

        private void Awake()
        {
            // Tạo pool SFX
            for (int i = 0; i < _sfxPoolSize; i++)
            {
                var go = new GameObject($"SFX_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.outputAudioMixerGroup = _sfxGroup;
                _sfxPool.Enqueue(src);
            }

            // Music source
            var musicGo = new GameObject("Music");
            musicGo.transform.SetParent(transform);
            _musicSource = musicGo.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.outputAudioMixerGroup = _musicGroup;

            // Load saved settings (hoặc dùng default cho lần đầu chơi)
            LoadSettings();
            ApplyAllVolumes();

            GameLog.Info(LogCategory.Audio,
                $"AudioManager ready. Master:{_masterVolume:F2}{(_masterMuted ? " (muted)" : "")}, " +
                $"Music:{_musicVolume:F2}{(_musicMuted ? " (muted)" : "")}, " +
                $"SFX:{_sfxVolume:F2}{(_sfxMuted ? " (muted)" : "")}");
        }

        private void LoadSettings()
        {
            _masterMuted = PlayerPrefs.GetInt(KeyMasterMuted, 0) == 1;
            _musicMuted = PlayerPrefs.GetInt(KeyMusicMuted, 0) == 1;
            _sfxMuted = PlayerPrefs.GetInt(KeySfxMuted, 0) == 1;
            _masterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, _defaultMasterVolume);
            _musicVolume = PlayerPrefs.GetFloat(KeyMusicVolume, _defaultMusicVolume);
            _sfxVolume = PlayerPrefs.GetFloat(KeySfxVolume, _defaultSfxVolume);
        }

        // ============ Apply volume to mixer ============

        private void ApplyAllVolumes()
        {
            ApplyMasterVolume();
            ApplyMusicVolume();
            ApplySfxVolume();
        }

        private void ApplyMasterVolume()
        {
            // Master muted -> volume 0 (override volume slider)
            float effective = _masterMuted ? 0f : _masterVolume;
            SetMixerVolume(_masterParam, effective);
        }

        private void ApplyMusicVolume()
        {
            // Music muted hoặc Master muted -> 0
            float effective = (_musicMuted || _masterMuted) ? 0f : _musicVolume;
            SetMixerVolume(_musicParam, effective);
        }

        private void ApplySfxVolume()
        {
            float effective = (_sfxMuted || _masterMuted) ? 0f : _sfxVolume;
            SetMixerVolume(_sfxParam, effective);
        }

        private void SetMixerVolume(string param, float linear)
        {
            if (_mixer == null) return;
            // 0..1 -> dB (log scale, audio người nghe đúng hơn)
            linear = Mathf.Clamp(linear, 0.0001f, 1f);
            _mixer.SetFloat(param, Mathf.Log10(linear) * 20f);
        }

        // ============ Playback API ============

        public void PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            // Skip nếu đang muted (đỡ tốn AudioSource cho không có tiếng)
            if (_masterMuted || _sfxMuted) return;

            // Rotate qua pool - oldest sound bị cắt nếu pool đầy
            var src = _sfxPool.Dequeue();
            src.clip = clip;
            src.volume = volume;
            src.pitch = pitch;
            src.Play();
            _sfxPool.Enqueue(src);
        }

        public void PlayMusic(AudioClip clip, bool loop = true, float fadeIn = 1f)
        {
            if (clip == null) return;
            if (_musicSource.clip == clip && _musicSource.isPlaying) return;

            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.volume = 0f;
            _musicSource.Play();
            StartCoroutine(Fade(_musicSource, 0f, 1f, fadeIn));
        }

        public void StopMusic(float fadeOut = 1f)
        {
            StartCoroutine(FadeOutAndStop(_musicSource, fadeOut));
        }

        private System.Collections.IEnumerator Fade(AudioSource src, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            src.volume = to;
        }

        private System.Collections.IEnumerator FadeOutAndStop(AudioSource src, float duration)
        {
            yield return Fade(src, src.volume, 0f, duration);
            src.Stop();
        }
    }
}
