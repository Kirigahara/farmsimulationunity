using UnityEngine;

namespace GameTemplate.Core.UI
{
    /// <summary>
    /// Auto-fit RectTransform vào Screen.safeArea - tránh notch, home indicator, status bar.
    ///
    /// Cách dùng:
    ///   1. Tạo Canvas full màn hình
    ///   2. Tạo GameObject con tên "SafeArea", set RectTransform stretch full parent
    ///   3. Add component này vào "SafeArea" GameObject
    ///   4. Mọi UI element (button, text, panel) đặt làm con của "SafeArea"
    ///
    /// Hierarchy mẫu:
    ///   Canvas (full màn hình - cho background tràn vào notch)
    ///   ├── Background (Image)         ← ngoài SafeArea, được phép tràn
    ///   └── SafeArea (SafeAreaFitter)  ← UI quan trọng đặt ở đây
    ///       ├── TopBar (HP, coins)
    ///       ├── GameplayUI
    ///       └── BottomBar (skill buttons)
    ///
    /// Tùy chọn padTop/padBottom/padLeft/padRight để control phía nào áp dụng padding.
    /// Vd: chỉ pad top (status bar) nhưng để bottom tràn vào home indicator (cho UI fullscreen game).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Header("Sides to apply safe area padding")]
        [SerializeField] private bool _padTop = true;
        [SerializeField] private bool _padBottom = true;
        [SerializeField] private bool _padLeft = true;
        [SerializeField] private bool _padRight = true;

        [Header("Debug")]
        [Tooltip("Log mỗi lần re-apply safe area (dev only).")]
        [SerializeField] private bool _logChanges = false;

        private RectTransform _rt;
        private Rect _lastSafeArea = Rect.zero;
        private Vector2Int _lastScreenSize = Vector2Int.zero;
        private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void OnEnable()
        {
            // Apply lại khi enable (vd: panel re-show sau khi xoay device)
            ApplySafeArea();
        }

        private void Update()
        {
            // Detect change: safe area, screen size, orientation đều có thể đổi runtime
            // (xoay device, split screen Android, foldable phone...)
            if (Screen.safeArea != _lastSafeArea
                || Screen.width != _lastScreenSize.x
                || Screen.height != _lastScreenSize.y
                || Screen.orientation != _lastOrientation)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();

            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastOrientation = Screen.orientation;

            // Screen.safeArea trả về Rect (đơn vị pixel) với origin góc trái-DƯỚI
            var safe = Screen.safeArea;

            // Convert sang anchor 0-1 của Canvas
            // Nếu side không pad → set về edge (0 hoặc 1) = không apply safe area phía đó
            Vector2 anchorMin = new Vector2(
                _padLeft ? safe.x / Screen.width : 0f,
                _padBottom ? safe.y / Screen.height : 0f
            );
            Vector2 anchorMax = new Vector2(
                _padRight ? (safe.x + safe.width) / Screen.width : 1f,
                _padTop ? (safe.y + safe.height) / Screen.height : 1f
            );

            // Edge case: trên Editor lúc đầu Screen.safeArea có thể trả 0 → tránh chia 0
            if (Screen.width <= 0 || Screen.height <= 0) return;

            _rt.anchorMin = anchorMin;
            _rt.anchorMax = anchorMax;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;

            if (_logChanges)
            {
                Debug.Log(
                    $"[SafeAreaFitter] Applied: screen={Screen.width}x{Screen.height}, " +
                    $"safe=({safe.x},{safe.y},{safe.width}x{safe.height}), " +
                    $"anchors=[{anchorMin}, {anchorMax}]",
                    this);
            }
        }

#if UNITY_EDITOR
        // Editor: re-apply khi đổi value trong Inspector
        private void OnValidate()
        {
            if (Application.isPlaying && _rt != null) ApplySafeArea();
        }
#endif
    }
}
