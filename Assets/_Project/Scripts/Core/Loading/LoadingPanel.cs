using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using GameTemplate.Core.Events;
using GameTemplate.Core.Patterns.Async;
using GameTemplate.Core.SceneManagement;
using GameTemplate.Core.DI;

namespace GameTemplate.Core
{
    public class LoadingPanel : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Slider _slider;
        [SerializeField] private GameObject _loadingPanel;   // ẩn/hiện cả panel

        [Header("Config")]
        [Tooltip("Thời gian tối thiểu hiển thị loading (giây).")]
        [SerializeField] private float _minLoadingSeconds = 3f;

        // ---------------------------------------------------------------
        private ISceneLoader _sceneLoader;
        private CancellationTokenSource _cts;

        // ---------------------------------------------------------------

        private void Awake()
        {
            // Lấy SceneLoader từ ServiceLocator
            _sceneLoader = ServiceLocator.Get<ISceneLoader>();

            // Subscribe events từ EventBus
            EventBus.Subscribe<SceneLoadStartedEvent>(OnSceneLoadStarted);
            EventBus.Subscribe<SceneLoadCompletedEvent>(OnSceneLoadCompleted);

            // Panel ẩn lúc đầu, chỉ hiện khi có event
            if (_loadingPanel != null) _loadingPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<SceneLoadStartedEvent>(OnSceneLoadStarted);
            EventBus.Unsubscribe<SceneLoadCompletedEvent>(OnSceneLoadCompleted);

            _cts?.Cancel();
            _cts?.Dispose();
        }

        // ---------------------------------------------------------------
        // Event handlers
        // ---------------------------------------------------------------

        private void OnSceneLoadStarted(SceneLoadStartedEvent e)
        {
            if (_loadingPanel != null) _loadingPanel.SetActive(true);
            if (_slider != null) _slider.value = 0f;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _ = RunSlider(_cts.Token);
        }

        private void OnSceneLoadCompleted(SceneLoadCompletedEvent e)
        {
            // Scene đã xong — slider tự kết thúc qua WaitUntil bên dưới
            // Panel sẽ ẩn sau khi slider chạy xong (xem RunSlider bước ④)
        }

        // ---------------------------------------------------------------
        // Core logic
        // ---------------------------------------------------------------

        /// <summary>
        /// Slider chạy từ 0 → 1, tốc độ = max(tiến độ thực tế, tiến độ theo thời gian tối thiểu).
        /// Đảm bảo slider không bao giờ chạy NHANH hơn _minLoadingSeconds,
        /// nhưng cũng không "đóng băng" nếu scene load chậm hơn kỳ vọng.
        /// </summary>
        private async Task RunSlider(CancellationToken ct)
        {
            float elapsed = 0f;

            // ① Chạy slider mỗi frame cho đến khi CẢ HAI điều kiện đúng:
            //    - Đã đủ _minLoadingSeconds giây
            //    - SceneLoader.Progress >= 1f (scene load xong)
            while (!ct.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;

                // Tiến độ theo thời gian tối thiểu (0 → 1 trong minSeconds)
                float timeProgress = Mathf.Clamp01(elapsed / _minLoadingSeconds);

                // Tiến độ thực từ SceneLoader (0 → 0.9 khi load, nhảy lên 1f khi done)
                // SceneLoader set Progress = 1f sau khi op.isDone
                float realProgress = _sceneLoader?.Progress ?? 0f;

                // Slider = giá trị NHỎ HƠN: không chạy nhanh hơn thực tế,
                // cũng không nhanh hơn thời gian tối thiểu
                float displayValue = Mathf.Min(timeProgress, realProgress);

                if (_slider != null) _slider.value = displayValue;

                // Thoát khi cả 2 đều đạt 1
                if (timeProgress >= 1f && realProgress >= 1f) break;

                await Task.Yield();
            }

            if (ct.IsCancellationRequested) return;

            // ② Snap slider = 1 cho chắc
            if (_slider != null) _slider.value = 1f;

            // ③ Delay nhỏ để player thấy slider đầy trước khi ẩn panel (tuỳ chọn)
            await AsyncOp.Delay(0.2f);

            // ④ Ẩn loading panel
            if (_loadingPanel != null) _loadingPanel.SetActive(false);
        }
    }
}
