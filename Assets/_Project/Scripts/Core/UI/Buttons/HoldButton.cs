using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GameTemplate.Core.DI;
using GameTemplate.Core.Mobile.Haptic;

namespace GameTemplate.Core.UI.Buttons
{
    /// <summary>
    /// Nút nhấn-giữ với 3 UnityEvent wire trực tiếp trong Inspector:
    ///   - OnStartAction: khi pointer down (vừa nhấn xuống)
    ///   - OnHoldAction: gọi mỗi frame khi đang giữ
    ///   - OnReleaseAction: khi pointer up (nhả tay) hoặc drag ra ngoài
    ///
    /// Use case:
    ///   - Charge attack: OnStart = start charging effect, OnHold = increase charge, OnRelease = fire
    ///   - Skip dialog: OnStart = show "đang skip", OnHold = (không cần), OnRelease = skip nếu giữ đủ lâu
    ///   - Move stick UI: OnStart = enable input, OnHold = read drag direction, OnRelease = stop
    ///   - Long-press menu: OnStart = (không cần), OnHold = show progress, OnRelease = trigger menu
    ///
    /// Wire trong Inspector:
    ///   - Drag GameObject vào field
    ///   - Chọn method (vd: PlayerController.StartCharging)
    ///   - Truyền parameter cố định nếu cần
    /// </summary>
    [AddComponentMenu("UI/Hold Button")]
    [RequireComponent(typeof(RectTransform))]
    public class HoldButton : Selectable,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        [Header("Events")]
        [Tooltip("Fire khi vừa nhấn xuống (pointer down).")]
        [SerializeField] private UnityEvent _onStartAction;

        [Tooltip("Fire mỗi frame khi đang giữ. Wire method nhận thông tin tăng dần.")]
        [SerializeField] private UnityEvent _onHoldAction;

        [Tooltip("Fire khi nhả tay (pointer up) hoặc drag ra ngoài button.")]
        [SerializeField] private UnityEvent _onReleaseAction;

        [Header("Settings")]
        [Tooltip("Nếu true, drag ngón tay ra khỏi button = fire Release. " +
                 "Nếu false, vẫn coi là giữ cho đến khi pointer up thực sự.")]
        [SerializeField] private bool _releaseOnPointerExit = true;

        [Header("Haptic (optional)")]
        [SerializeField] private HapticType _hapticOnStart = HapticType.Light;
        [SerializeField] private HapticType _hapticOnRelease = HapticType.None;

        // Runtime state
        private bool _isHolding = false;
        private float _holdStartTime;

        // ===== Public read-only =====
        /// <summary>True khi đang giữ button.</summary>
        public bool IsHolding => _isHolding;

        /// <summary>Thời gian đã giữ (giây) - 0 nếu không đang giữ.</summary>
        public float HoldDuration => _isHolding ? Time.unscaledTime - _holdStartTime : 0f;

        /// <summary>Public access events nếu code muốn AddListener thay vì wire Inspector.</summary>
        public UnityEvent OnStartAction => _onStartAction;
        public UnityEvent OnHoldAction => _onHoldAction;
        public UnityEvent OnReleaseAction => _onReleaseAction;

        // ============================================================
        // POINTER EVENTS
        // ============================================================
        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable()) return;
            base.OnPointerDown(eventData);
            StartHold();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isHolding) return;
            EndHold();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (_isHolding && _releaseOnPointerExit)
            {
                EndHold();
            }
        }

        // ============================================================
        // HOLD LIFECYCLE
        // ============================================================
        private void StartHold()
        {
            _isHolding = true;
            _holdStartTime = Time.unscaledTime;

            // Haptic
            if (_hapticOnStart != HapticType.None
                && ServiceLocator.TryGet<IHapticService>(out var haptic))
            {
                haptic.Play(_hapticOnStart);
            }

            _onStartAction?.Invoke();
        }

        private void EndHold()
        {
            _isHolding = false;

            if (_hapticOnRelease != HapticType.None
                && ServiceLocator.TryGet<IHapticService>(out var haptic))
            {
                haptic.Play(_hapticOnRelease);
            }

            _onReleaseAction?.Invoke();
        }

        // ============================================================
        // UPDATE - fire OnHold mỗi frame khi đang giữ
        // ============================================================
        private void Update()
        {
            if (!_isHolding) return;
            _onHoldAction?.Invoke();
        }

        // ============================================================
        // LIFECYCLE - cleanup khi disable (chống leak)
        // ============================================================
        protected override void OnDisable()
        {
            base.OnDisable();
            // Nếu đang giữ mà bị disable → fire Release để cleanup
            if (_isHolding) EndHold();
        }
    }
}
