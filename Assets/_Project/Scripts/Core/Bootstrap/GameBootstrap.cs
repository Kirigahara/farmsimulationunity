using UnityEngine;
using UnityEngine.SceneManagement;
using GameTemplate.Core.DI;
using GameTemplate.Core.Save;
using GameTemplate.Core.SceneManagement;
using GameTemplate.Core.Audio;
using GameTemplate.Core.UI;
using GameTemplate.Core.Logger;

namespace GameTemplate.Core.Bootstrap
{
    /// <summary>
    /// Entry point của game. Đặt 1 GameObject với script này vào scene "Bootstrap"
    /// và set Bootstrap là scene đầu tiên trong Build Settings.
    ///
    /// Flow:
    ///   1. Khởi tạo các service (Save, SceneLoader)
    ///   2. Gán prefab manager (Audio, UI) - bằng Inspector
    ///   3. Register tất cả vào ServiceLocator
    ///   4. Load Main Menu
    ///
    /// Lợi ích: Test scene gameplay riêng vẫn được vì Bootstrap detect và auto-skip
    /// nếu service đã register (khi dev play từ scene giữa).
    /// </summary>
    [DefaultExecutionOrder(-1000)] // Chạy trước mọi script khác
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Manager Prefabs")]
        [SerializeField] private AudioManager _audioManagerPrefab;
        [SerializeField] private UIManager _uiManagerPrefab;

        [Header("Mobile Settings")]
        [SerializeField] private int _targetFrameRate = 60;
        [SerializeField] private bool _multiTouchEnabled = false;

        [Header("Flow")]
        [SerializeField] private string _firstSceneName = "MainMenu";

        private void Awake()
        {
            // Tránh double init khi reload scene
            if (ServiceLocator.TryGet<ISaveService>(out _))
            {
                GameLog.Info(LogCategory.Bootstrap, "Already bootstrapped, skipping.");
                return;
            }

            DontDestroyOnLoad(gameObject);
            ConfigureMobile();
            RegisterServices();
            LoadFirstScene();
        }

        private void ConfigureMobile()
        {
            Application.targetFrameRate = _targetFrameRate;
            UnityEngine.Input.multiTouchEnabled = _multiTouchEnabled;
            // Tắt screen sleep trong gameplay - đa số mobile game cần
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            GameLog.Info(LogCategory.Bootstrap, $"Mobile config: {_targetFrameRate}fps");
        }

        private void RegisterServices()
        {
            // Pure C# services - new trực tiếp
            ServiceLocator.Register<ISaveService>(new JsonSaveService());
            ServiceLocator.Register<ISceneLoader>(new SceneLoader());

            // MonoBehaviour services - instantiate prefab và DontDestroyOnLoad
            if (_audioManagerPrefab != null)
            {
                var audio = Instantiate(_audioManagerPrefab, transform);
                ServiceLocator.Register<IAudioService>(audio);
            }

            if (_uiManagerPrefab != null)
            {
                var ui = Instantiate(_uiManagerPrefab, transform);
                ServiceLocator.Register(ui);
            }

            GameLog.Info(LogCategory.Bootstrap, "All services registered.");
        }

        private async void LoadFirstScene()
        {
            // Nếu đang ở Bootstrap scene -> load Main Menu.
            // Nếu test từ scene khác -> giữ nguyên (skip để dev không bị throw về Main Menu).
            if (SceneManager.GetActiveScene().name == "Bootstrap")
            {
                var loader = ServiceLocator.Get<ISceneLoader>();
                await loader.LoadSceneAsync(_firstSceneName, showLoading: false);
            }
        }
    }
}
