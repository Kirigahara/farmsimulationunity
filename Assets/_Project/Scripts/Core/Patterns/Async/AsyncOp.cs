using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GameTemplate.Core.Patterns.Async
{
    /// <summary>
    /// Async helpers - làm việc với coroutine và async không đau đầu.
    /// Không phụ thuộc UniTask hay DOTween (đỡ phải import plugin).
    /// Nếu sau này import UniTask thì migrate dễ vì API tương tự.
    ///
    /// Hỗ trợ:
    ///   - Task.Delay(seconds) trong Unity scaled time
    ///   - Chain animation: move -> scale -> fade
    ///   - Wait until condition true
    ///   - Run nhiều task song song và await all
    /// </summary>
    public static class AsyncOp
    {
        public static Action _LoopAction;

        /// <summary>Delay theo Time.timeScale (pause khi game pause).</summary>
        public static async Task Delay(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                await Task.Yield();
            }
        }

        /// <summary>
        /// Delay theo Time.timeScale (pause khi game pause), sau đó gọi onComplete.
        /// </summary>
        /// <param name="seconds">Số giây để delay.</param>
        /// <param name="onComplete">Hàm callback sẽ được gọi sau khi delay kết thúc.</param>
        /// <returns></returns>
        public static async Task Delay(float seconds, Action onComplete)
        {
            await Delay(seconds);
            onComplete?.Invoke();
        }

        /// <summary>Delay theo realtime (không pause khi game pause - dùng cho UI).</summary>
        public static async Task DelayRealtime(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                await Task.Yield();
            }
        }

        /// <summary>Đợi đến khi condition trả về true.</summary>
        public static async Task WaitUntil(Func<bool> condition, float timeout = -1f)
        {
            float t = 0f;
            while (!condition())
            {
                if (timeout > 0 && t >= timeout)
                {
                    Debug.LogWarning($"[AsyncOp.WaitUntil] Timeout sau {timeout}s.");
                    return;
                }
                t += Time.deltaTime;
                await Task.Yield();
            }
        }

        /// <summary>Tween float từ from -> to trong duration giây, gọi onUpdate mỗi frame.</summary>
        public static async Task Tween(float from, float to, float duration, Action<float> onUpdate, AnimationCurve curve = null)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float normalized = Mathf.Clamp01(t / duration);
                float eased = curve != null ? curve.Evaluate(normalized) : normalized;
                onUpdate?.Invoke(Mathf.Lerp(from, to, eased));
                await Task.Yield();
            }
            onUpdate?.Invoke(to); // đảm bảo final value chính xác
        }

        /// <summary>Tween float từ from -> to trong duration giây, gọi onUpdate mỗi frame, gọi onComplete khi kết thúc.</summary>
        public static async Task Tween(
            float from, float to, float duration,
            Action<float> onUpdate, Action onComplete,
            AnimationCurve curve = null)
        {
            await Tween(from, to, duration, onUpdate, curve);
            onComplete?.Invoke();
        }

        /// <summary>
        /// Tween vector3 từ from -> to trong duration giây, gọi onUpdate mỗi frame, gọi onComplete khi kết thúc
        /// </summary>
        public static async Task MoveTween(
            Vector3 from, Vector3 to, float duration,
            Action<Vector3> onUpdate, Action onComplete,
            AnimationCurve curve = null)
        {
            float t = 0f;
            Vector3 offset = to - from;

            while (t < duration)
            {
                t += Time.deltaTime;
                float normalized = Mathf.Clamp01(t / duration);
                float eased = curve != null ? curve.Evaluate(normalized) : normalized;
                Vector3 pos = from + offset * eased;
                onUpdate?.Invoke(pos);
                await Task.Yield();
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// Tween vector3 từ from -> to trong duration giây, gọi onUpdate mỗi frame, gọi onComplete khi kết thúc <\br>
        /// Sử dụng khi đích đến là vật thể động, theo dấu vị trí của đích đến liên tục
        /// </summary>
        public static async Task MoveTween(
            Vector3 from, Transform to, float duration,
            Action<Vector3> onUpdate, Action onComplete,
            AnimationCurve curve = null)
        {
            float t = 0f;
            Vector3 offset = to.position - from;

            while (t < duration)
            {
                offset = to.position - from;
                t += Time.deltaTime;
                float normalized = Mathf.Clamp01(t / duration);
                float eased = curve != null ? curve.Evaluate(normalized) : normalized;
                Vector3 pos = from + offset * eased;
                onUpdate?.Invoke(pos);
                await Task.Yield();
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// Chạy nhiều task song song.
        /// Vd: await AsyncOp.WhenAll(fadeUI(), playMusic(), loadScene());
        /// </summary>
        public static Task WhenAll(params Task[] tasks) => Task.WhenAll(tasks);

        // ============================================================
        // REPEAT ACTION - thay thế InvokeRepeating
        // ============================================================

        /// <summary>
        /// Action mặc định cho overload <see cref="RepeatAction(float)"/>.
        /// Set field này trước khi gọi RepeatAction(interval).
        ///
        /// ⚠️ Static = chỉ dùng cho 1 loop duy nhất. Nhiều loop song song dùng overload có Action parameter.
        /// </summary>
        public static Action LoopAction;

        /// <summary>
        /// CancellationTokenSource mặc định cho overload <see cref="RepeatAction(float)"/>.
        /// Tự được tạo mới mỗi lần gọi RepeatAction(interval) - lần gọi cũ sẽ bị cancel auto.
        ///
        /// Để cancel: gọi <c>AsyncOp.LoopCts.Cancel()</c>.
        /// </summary>
        public static CancellationTokenSource LoopCts;

        /// <summary>
        /// Quick use - lặp lại <see cref="LoopAction"/> mỗi <paramref name="intervalSeconds"/> giây.
        /// Action chạy NGAY lập tức lần đầu, sau đó mới đợi interval.
        ///
        /// Cách dùng:
        /// <code>
        /// AsyncOp.LoopAction = () => Debug.Log("tick");
        /// _ = AsyncOp.RepeatAction(1f);
        /// // ...
        /// AsyncOp.LoopCts.Cancel();  // dừng
        /// </code>
        ///
        /// ⚠️ Dùng static field nên CHỈ chạy 1 loop tại 1 thời điểm.
        /// Nếu cần nhiều loop song song → dùng overload <see cref="RepeatAction(float, Action, CancellationToken)"/>.
        /// </summary>
        public static Task RepeatAction(float intervalSeconds)
        {
            // Cancel loop cũ nếu còn (tránh 2 loop chạy song song lúc set field mới)
            LoopCts?.Cancel();
            LoopCts?.Dispose();
            LoopCts = new CancellationTokenSource();

            return RepeatAction(intervalSeconds, LoopAction, LoopCts.Token, runImmediately: true);
        }

        /// <summary>
        /// Production use - lặp lại <paramref name="action"/> mỗi <paramref name="intervalSeconds"/> giây.
        /// Cancellable, exception-safe, không kill loop khi action throw.
        ///
        /// Cách dùng:
        /// <code>
        /// var cts = new CancellationTokenSource();
        /// _ = AsyncOp.RepeatAction(2f, SpawnEnemy, cts.Token);
        /// // ...
        /// cts.Cancel();
        /// </code>
        /// </summary>
        public static async Task RepeatAction(
            float intervalSeconds,
            Action action,
            CancellationToken cancellationToken = default,
            bool runImmediately = true)
        {
            if (action == null) return;
            if (intervalSeconds <= 0)
            {
                Debug.LogWarning(
                    $"[AsyncOp.RepeatAction] intervalSeconds <= 0 ({intervalSeconds}) - skipping.");
                return;
            }

            if (runImmediately)
            {
                try { action.Invoke(); }
                catch (Exception ex)
                {
                    Debug.LogError($"[AsyncOp.RepeatAction] Action throw: {ex.Message}\n{ex.StackTrace}");
                }
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                await Delay(intervalSeconds);
                if (cancellationToken.IsCancellationRequested) return;

                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    // 1 lần fail không kill loop - log rồi tiếp tục
                    Debug.LogError($"[AsyncOp.RepeatAction] Action throw: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
    }

    /// <summary>
    /// Sequence: chain hoạt động tuần tự.
    /// Cách dùng:
    ///   await new Sequence()
    ///       .Append(() => AsyncOp.Tween(0, 1, 0.3f, v => panel.alpha = v))
    ///       .Append(() => AsyncOp.Delay(1f))
    ///       .Append(() => AsyncOp.Tween(1, 0, 0.3f, v => panel.alpha = v))
    ///       .Play();
    /// </summary>
    public class Sequence
    {
        private readonly List<Func<Task>> _steps = new List<Func<Task>>();

        public Sequence Append(Func<Task> step)
        {
            _steps.Add(step);
            return this;
        }

        public Sequence AppendDelay(float seconds)
        {
            _steps.Add(() => AsyncOp.Delay(seconds));
            return this;
        }

        public Sequence AppendCallback(Action callback)
        {
            _steps.Add(() => { callback?.Invoke(); return Task.CompletedTask; });
            return this;
        }

        public async Task Play()
        {
            foreach (var step in _steps)
                await step();
        }
    }

    /// <summary>
    /// CoroutineRunner - chạy coroutine từ pure C# class (vd Presenter, Service).
    /// Tự tạo 1 GameObject ẩn để host coroutine.
    /// </summary>
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;

        public static CoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[CoroutineRunner]");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    _instance = go.AddComponent<CoroutineRunner>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public new Coroutine StartCoroutine(IEnumerator routine) => base.StartCoroutine(routine);
        public new void StopCoroutine(Coroutine routine) => base.StopCoroutine(routine);
    }
}
