using GameTemplate.Core.DI;
using GameTemplate.Core.Logger;
using GameTemplate.Core.Patterns.Async;
using GameTemplate.Core.SceneManagement;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Android.Gradle;
using UnityEngine;
using UnityEngine.UI;

namespace GameTemplate.Core
{
    /// <summary>
    /// Controller cho SCENE LOADING độc lập (không phải loading panel overlay).
    ///
    /// Scene này chạy một lần duy nhất lúc khởi động game, đảm nhiệm:
    ///   1. Preload AssetBundle (chưa implement, để sẵn hook <see cref="PreloadAssetsAsync"/>).
    ///   2. Chạy slider tối thiểu <see cref="_minLoadingSeconds"/> giây.
    ///   3. Chuyển thẳng sang <see cref="_nextSceneName"/> khi xong.
    ///
    /// Khác với LoadingSceneController (panel overlay):
    ///   - KHÔNG dùng EventBus — tự điều phối toàn bộ flow.
    ///   - KHÔNG cần caller bên ngoài gọi LoadSceneAsync — tự chạy trong Start().
    ///   - Chỉ định thẳng scene đích qua <see cref="_nextSceneName"/>.
    /// </summary>

    public class LoadingScene : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Slider _slider;

        [Header("Config")]
        [Tooltip("Thời gian tối thiểu hiển thị loading screen (giây).")]
        [SerializeField] private float _minLoadingSeconds = 3f;

        [Tooltip("Scene chuyển đến sau khi load xong (phải có trong Build Settings).")]
        [SerializeField] private string _nextSceneName = "MainMenu";

        // ---------------------------------------------------------------
        private ISceneLoader _sceneLoader;
        private CancellationTokenSource _cts;

        // ---------------------------------------------------------------
        public static Func<Task> OnPreload;

        private void Start()
        {
            _sceneLoader = ServiceLocator.Get<ISceneLoader>();

            _cts = new CancellationTokenSource();

            _ = RunBootstrapAsync(_cts.Token);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        // ---------------------------------------------------------------
        // Main flow
        // ---------------------------------------------------------------

        private async Task PreloadLocalAssetsAsync(CancellationToken ct)
        {
            if (OnPreload != null)
                await OnPreload.Invoke();
        }

        private async Task RunBootstrapAsync(CancellationToken ct)
        {
            if (_slider != null) _slider.value = 0f;

            //  Preload Local Data + chạy timer song song
            await AsyncOp.WhenAll(
                PreloadLocalAssetsAsync(ct),
                RunTimerAsync(ct)
            );

            // ① Preload AssetBundle + chạy timer song song
            //    Cả hai chạy cùng lúc — đợi cả hai xong mới tiếp tục.
            await AsyncOp.WhenAll(
                PreloadAssetsAsync(ct),
                RunTimerAsync(ct)
            );

            if (ct.IsCancellationRequested) return;

            // ② Snap slider = 1 trước khi chuyển scene
            if (_slider != null) _slider.value = 1f;

            // ③ Delay nhỏ để player thấy slider đầy
            await AsyncOp.Delay(0.3f);

            if (ct.IsCancellationRequested) return;

            // ④ Chuyển scene
            GameLog.Info(LogCategory.Scene, $"[Bootstrap] Chuyển sang scene: {_nextSceneName}");
            await _sceneLoader.LoadSceneAsync(_nextSceneName, showLoading: false);
        }

        // ---------------------------------------------------------------
        // Timer: đếm đủ _minLoadingSeconds, cập nhật slider mỗi frame
        // ---------------------------------------------------------------

        private async Task RunTimerAsync(CancellationToken ct)
        {
            await AsyncOp.Tween(
                from: 0f,
                to: 1f,
                duration: _minLoadingSeconds,
                onUpdate: v =>
                {
                    // Chỉ update slider nếu chưa có asset progress override
                    // (khi có AssetBundle thì _assetProgress sẽ được dùng thay)
                    if (_slider != null)
                        _slider.value = Mathf.Max(_slider.value, v);
                }
            );
        }

        // ---------------------------------------------------------------
        // Asset preload hook — thêm logic AssetBundle vào đây sau
        // ---------------------------------------------------------------

        private async Task LoadBundleA(CancellationToken ct)
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "bundle_a");

            var request = AssetBundle.LoadFromFileAsync(path);

            // Đợi từng frame cho đến khi load xong
            while (!request.isDone)
            {
                if (ct.IsCancellationRequested) return;
                await Task.Yield();
            }

            if (request.assetBundle == null)
            {
                GameLog.Error(LogCategory.Scene, "[Bootstrap] Load bundle_a thất bại.");
                return;
            }

            // Cache lại để dùng sau (tuỳ design của bạn)
            AssetBundle _bundleA = request.assetBundle;
        }

        /// <summary>
        /// Preload AssetBundle và các resource cần thiết trước khi vào game.
        ///
        /// TODO: implement AssetBundle loading ở đây.
        /// Khi có progress thực, gọi <see cref="SetAssetProgress"/> để cập nhật slider.
        ///
        /// Ví dụ sau khi có AssetBundle:
        /// <code>
        /// var bundle = await AssetBundle.LoadFromFileAsync(path);
        /// SetAssetProgress(0.5f);
        /// await bundle.LoadAllAssetsAsync();
        /// SetAssetProgress(1f);
        /// </code>
        /// </summary>
        private async Task PreloadAssetsAsync(CancellationToken ct)
        {
            // --- PLACEHOLDER: chưa có AssetBundle, return ngay ---
            // Khi implement, xoá dòng dưới và thêm logic thực tế vào đây.
            await Task.CompletedTask;

            // Ví dụ structure khi có AssetBundle:
            // SetAssetProgress(0f);
            // await LoadBundleA(ct); SetAssetProgress(0.33f);
            // await LoadBundleB(ct); SetAssetProgress(0.66f);
            // await LoadBundleC(ct); SetAssetProgress(1.0f);
        }

        /// <summary>
        /// Gọi từ <see cref="PreloadAssetsAsync"/> để đẩy slider theo tiến độ asset thực tế.
        /// Slider chỉ tiến về phía trước, không lùi.
        /// </summary>
        private void SetAssetProgress(float progress)
        {
            if (_slider != null)
                _slider.value = Mathf.Max(_slider.value, Mathf.Clamp01(progress));
        }

    }
}
