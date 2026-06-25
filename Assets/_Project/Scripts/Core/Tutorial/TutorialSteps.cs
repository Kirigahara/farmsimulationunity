using System;
using UnityEngine;
using GameTemplate.Core.Events;

namespace GameTemplate.Core.Tutorial
{
    // ============================================================
    // STEP 1: MessageStep - chỉ hiện text bubble, chờ tap dismiss
    // ============================================================
    /// <summary>
    /// Hiện 1 text bubble giữa màn hình, chờ user tap để tiếp tục.
    /// Dùng cho: intro dialog, lời chào, giải thích cốt truyện.
    /// Không highlight gì.
    /// </summary>
    public class MessageStep : ITutorialStep
    {
        public string StepId { get; }
        private readonly string _message;
        private readonly BubblePlacement _placement;
        private TutorialContext _ctx;

        public MessageStep(string stepId, string message, BubblePlacement placement = BubblePlacement.Center)
        {
            StepId = stepId;
            _message = message;
            _placement = placement;
        }

        public void Enter(TutorialContext context)
        {
            _ctx = context;
            _ctx.Overlay.ClearHighlight(); // mask phủ kín, không khoét lỗ
            _ctx.Overlay.ShowMessage(_message, _placement, showTapHint: true);
        }

        public bool IsComplete() => _ctx.Overlay.WasTappedThisFrame();

        public void Exit() => _ctx.Overlay.HideMessage();
    }

    // ============================================================
    // STEP 2: WaitForClickStep - highlight button + chờ user bấm
    // ============================================================
    /// <summary>
    /// Highlight 1 UI element và chờ user bấm vào đúng element đó.
    /// Block input mọi chỗ khác → user buộc phải bấm đúng chỗ.
    /// Dùng cho: "Bấm nút Shop", "Bấm nút Play", "Mở túi đồ".
    /// </summary>
    public class WaitForClickStep : ITutorialStep
    {
        public string StepId { get; }
        private readonly Func<RectTransform> _targetGetter;
        private readonly string _message;
        private readonly BubblePlacement _placement;
        private TutorialContext _ctx;

        /// <param name="targetGetter">Func trả về RectTransform - dùng Func để lazy resolve
        /// (element có thể chưa tồn tại lúc tạo step).</param>
        public WaitForClickStep(
            string stepId,
            Func<RectTransform> targetGetter,
            string message,
            BubblePlacement placement = BubblePlacement.Auto)
        {
            StepId = stepId;
            _targetGetter = targetGetter;
            _message = message;
            _placement = placement;
        }

        public void Enter(TutorialContext context)
        {
            _ctx = context;
            var target = _targetGetter?.Invoke();
            if (target != null)
            {
                _ctx.Overlay.HighlightTarget(target, allowClickThrough: true);
            }
            if (!string.IsNullOrEmpty(_message))
            {
                _ctx.Overlay.ShowMessage(_message, _placement);
            }
        }

        public bool IsComplete() => _ctx.Overlay.WasHighlightTappedThisFrame();

        public void Exit()
        {
            _ctx.Overlay.ClearHighlight();
            _ctx.Overlay.HideMessage();
        }
    }

    // ============================================================
    // STEP 3: WaitForEventStep<T> - chờ EventBus event fire
    // ============================================================
    /// <summary>
    /// Hiện hướng dẫn (optional highlight) rồi chờ 1 EventBus event fire.
    /// Dùng cho: "Giết enemy đầu tiên" (chờ EnemyKilledEvent),
    ///           "Thu thập coin" (chờ CoinCollectedEvent).
    ///
    /// Engine không biết event là gì - game tự define event + pass vào generic.
    /// </summary>
    public class WaitForEventStep<TEvent> : ITutorialStep where TEvent : struct, IGameEvent
    {
        public string StepId { get; }
        private readonly string _message;
        private readonly Func<RectTransform> _highlightGetter;
        private readonly Func<TEvent, bool> _filter;
        private TutorialContext _ctx;
        private bool _eventFired;

        /// <param name="filter">Optional - chỉ complete khi event thoả filter.
        /// Vd: chờ giết enemy "boss" cụ thể: e => e.EnemyType == "boss".</param>
        public WaitForEventStep(
            string stepId,
            string message,
            Func<RectTransform> highlightGetter = null,
            Func<TEvent, bool> filter = null)
        {
            StepId = stepId;
            _message = message;
            _highlightGetter = highlightGetter;
            _filter = filter;
        }

        public void Enter(TutorialContext context)
        {
            _ctx = context;
            _eventFired = false;

            var highlight = _highlightGetter?.Invoke();
            if (highlight != null)
            {
                // allowClickThrough cho phép user tương tác gameplay để hoàn thành
                _ctx.Overlay.HighlightTarget(highlight, allowClickThrough: true);
            }
            else
            {
                _ctx.Overlay.ClearHighlight();
            }

            if (!string.IsNullOrEmpty(_message))
            {
                _ctx.Overlay.ShowMessage(_message, BubblePlacement.Auto);
            }

            EventBus.Subscribe<TEvent>(OnEvent);
        }

        private void OnEvent(TEvent evt)
        {
            if (_filter == null || _filter(evt))
            {
                _eventFired = true;
            }
        }

        public bool IsComplete() => _eventFired;

        public void Exit()
        {
            EventBus.Unsubscribe<TEvent>(OnEvent);
            _ctx.Overlay.ClearHighlight();
            _ctx.Overlay.HideMessage();
        }
    }

    // ============================================================
    // STEP 4: WaitForConditionStep - chờ điều kiện custom
    // ============================================================
    /// <summary>
    /// Hiện hướng dẫn rồi chờ 1 điều kiện (Func bool) thoả.
    /// Dùng cho: "Di chuyển nhân vật" (chờ player.position đổi),
    ///           "Lên cấp 2" (chờ player.Level >= 2),
    ///           "Kéo joystick" (chờ input magnitude > 0.5).
    ///
    /// Linh hoạt nhất - mọi điều kiện game-specific express qua lambda.
    /// </summary>
    public class WaitForConditionStep : ITutorialStep
    {
        public string StepId { get; }
        private readonly Func<bool> _condition;
        private readonly string _message;
        private readonly Func<RectTransform> _highlightGetter;
        private TutorialContext _ctx;

        public WaitForConditionStep(
            string stepId,
            Func<bool> condition,
            string message,
            Func<RectTransform> highlightGetter = null)
        {
            StepId = stepId;
            _condition = condition;
            _message = message;
            _highlightGetter = highlightGetter;
        }

        public void Enter(TutorialContext context)
        {
            _ctx = context;

            var highlight = _highlightGetter?.Invoke();
            if (highlight != null)
                _ctx.Overlay.HighlightTarget(highlight, allowClickThrough: true);
            else
                _ctx.Overlay.ClearHighlight();

            if (!string.IsNullOrEmpty(_message))
                _ctx.Overlay.ShowMessage(_message, BubblePlacement.Auto);
        }

        public bool IsComplete() => _condition != null && _condition();

        public void Exit()
        {
            _ctx.Overlay.ClearHighlight();
            _ctx.Overlay.HideMessage();
        }
    }
}
