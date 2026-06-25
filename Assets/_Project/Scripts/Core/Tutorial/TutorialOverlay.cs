using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GameTemplate.Core.Tutorial
{
    /// <summary>
    /// Implementation UI của ITutorialOverlay.
    ///
    /// Cấu trúc UI (setup trong prefab):
    ///   TutorialOverlay (Canvas - sort order cao, phủ mọi UI khác)
    ///   ├── DimMask (Image đen mờ - block raycast)
    ///   │   └── HighlightHole (RectTransform - vùng khoét lỗ, cho click through)
    ///   ├── Pointer (Image - tay/mũi tên chỉ vào target, optional)
    ///   └── MessageBubble (panel chứa text hướng dẫn)
    ///       ├── MessageText (Text)
    ///       └── TapHint (Text - "Tap để tiếp tục")
    ///
    /// Cơ chế highlight bằng 4 mask panel (không cần shader):
    ///   Chia màn hình thành 4 vùng tối quanh lỗ highlight (trên/dưới/trái/phải).
    ///   Vùng giữa = lỗ trống → thấy element + click through được.
    /// </summary>
    public class TutorialOverlay : MonoBehaviour, ITutorialOverlay, IPointerClickHandler
    {
        [Header("Root")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private GameObject _root;

        [Header("Dim Mask (4 panel quây quanh lỗ)")]
        [Tooltip("4 Image đen mờ: Top, Bottom, Left, Right. Khoảng giữa = lỗ highlight.")]
        [SerializeField] private RectTransform _maskTop;
        [SerializeField] private RectTransform _maskBottom;
        [SerializeField] private RectTransform _maskLeft;
        [SerializeField] private RectTransform _maskRight;
        [Tooltip("Full mask khi không highlight gì (phủ kín màn hình).")]
        [SerializeField] private Image _fullMask;

        [Header("Pointer (optional)")]
        [SerializeField] private RectTransform _pointer;

        [Header("Message Bubble")]
        [SerializeField] private RectTransform _messageBubble;
        [SerializeField] private Text _messageText;
        [SerializeField] private GameObject _tapHint;

        [Header("Block Input")]
        [Tooltip("GraphicRaycaster + Image trên DimMask để chặn click ngoài lỗ.")]
        [SerializeField] private GraphicRaycaster _raycaster;

        // Runtime state
        private RectTransform _canvasRect;
        private RectTransform _currentHighlight;
        private bool _allowClickThrough;
        private bool _tappedThisFrame;
        private bool _highlightTappedThisFrame;
        private UnityEngine.Camera _worldCamera;

        private void Awake()
        {
            _canvasRect = _canvas.GetComponent<RectTransform>();
            _worldCamera = UnityEngine.Camera.main;
            if (_root != null) _root.SetActive(false);
        }

        private void LateUpdate()
        {
            // Reset tap flag mỗi frame (set trong OnPointerClick)
            _tappedThisFrame = false;
            _highlightTappedThisFrame = false;
        }

        // ============================================================
        // SHOW / HIDE
        // ============================================================
        public void Show()
        {
            if (_root != null) _root.SetActive(true);
            ClearHighlight();
            HideMessage();
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        // ============================================================
        // HIGHLIGHT
        // ============================================================
        public void HighlightTarget(RectTransform target, bool allowClickThrough = true, float padding = 10f)
        {
            if (target == null) { ClearHighlight(); return; }

            _currentHighlight = target;
            _allowClickThrough = allowClickThrough;

            // Tính bounds của target trên screen space
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);

            // Convert sang local space của canvas
            Vector2 min = WorldToCanvasPoint(corners[0]);
            Vector2 max = WorldToCanvasPoint(corners[2]);

            min -= Vector2.one * padding;
            max += Vector2.one * padding;

            // Ẩn full mask, hiện 4 mask quây quanh lỗ
            if (_fullMask != null) _fullMask.gameObject.SetActive(false);
            ArrangeHoleMasks(min, max);
        }

        public void HighlightWorldPosition(Vector3 worldPosition, float screenRadius = 80f, bool allowClickThrough = false)
        {
            _currentHighlight = null;
            _allowClickThrough = allowClickThrough;

            if (_worldCamera == null) _worldCamera = UnityEngine.Camera.main;
            if (_worldCamera == null) return;

            // World → screen → canvas
            Vector3 screenPos = _worldCamera.WorldToScreenPoint(worldPosition);
            Vector2 canvasPos = ScreenToCanvasPoint(screenPos);

            Vector2 min = canvasPos - Vector2.one * screenRadius;
            Vector2 max = canvasPos + Vector2.one * screenRadius;

            if (_fullMask != null) _fullMask.gameObject.SetActive(false);
            ArrangeHoleMasks(min, max);
        }

        public void ClearHighlight()
        {
            _currentHighlight = null;

            // Hiện full mask (phủ kín), ẩn 4 hole mask
            if (_fullMask != null) _fullMask.gameObject.SetActive(true);
            SetHoleMasksActive(false);
            if (_pointer != null) _pointer.gameObject.SetActive(false);
        }

        /// <summary>
        /// Sắp xếp 4 mask panel để tạo "lỗ" giữa (min..max là vùng trống).
        /// </summary>
        private void ArrangeHoleMasks(Vector2 holeMin, Vector2 holeMax)
        {
            if (_maskTop == null) return; // chưa setup 4 mask

            SetHoleMasksActive(true);

            float canvasW = _canvasRect.rect.width;
            float canvasH = _canvasRect.rect.height;
            float halfW = canvasW / 2f;
            float halfH = canvasH / 2f;

            // Canvas pivot center → tọa độ từ -half đến +half
            // Top mask: từ holeMax.y lên trên cùng
            SetMaskRect(_maskTop, -halfW, holeMax.y, halfW, halfH);
            // Bottom mask: từ dưới cùng đến holeMin.y
            SetMaskRect(_maskBottom, -halfW, -halfH, halfW, holeMin.y);
            // Left mask: giữa chiều dọc lỗ, từ trái đến holeMin.x
            SetMaskRect(_maskLeft, -halfW, holeMin.y, holeMin.x, holeMax.y);
            // Right mask: giữa chiều dọc lỗ, từ holeMax.x đến phải
            SetMaskRect(_maskRight, holeMax.x, holeMin.y, halfW, holeMax.y);

            // Pointer chỉ vào giữa lỗ
            if (_pointer != null)
            {
                _pointer.gameObject.SetActive(true);
                Vector2 center = (holeMin + holeMax) / 2f;
                _pointer.anchoredPosition = new Vector2(center.x, holeMin.y - 40f); // dưới lỗ chỉ lên
            }
        }

        private void SetMaskRect(RectTransform mask, float left, float bottom, float right, float top)
        {
            if (mask == null) return;
            mask.anchorMin = new Vector2(0.5f, 0.5f);
            mask.anchorMax = new Vector2(0.5f, 0.5f);
            mask.pivot = new Vector2(0.5f, 0.5f);
            float width = right - left;
            float height = top - bottom;
            mask.sizeDelta = new Vector2(width, height);
            mask.anchoredPosition = new Vector2(left + width / 2f, bottom + height / 2f);
        }

        private void SetHoleMasksActive(bool active)
        {
            if (_maskTop != null) _maskTop.gameObject.SetActive(active);
            if (_maskBottom != null) _maskBottom.gameObject.SetActive(active);
            if (_maskLeft != null) _maskLeft.gameObject.SetActive(active);
            if (_maskRight != null) _maskRight.gameObject.SetActive(active);
        }

        // ============================================================
        // MESSAGE BUBBLE
        // ============================================================
        public void ShowMessage(string message, BubblePlacement placement = BubblePlacement.Auto, bool showTapHint = false)
        {
            if (_messageBubble == null) return;

            _messageBubble.gameObject.SetActive(true);
            if (_messageText != null) _messageText.text = message;
            if (_tapHint != null) _tapHint.SetActive(showTapHint);

            PositionBubble(placement);
        }

        public void HideMessage()
        {
            if (_messageBubble != null) _messageBubble.gameObject.SetActive(false);
        }

        private void PositionBubble(BubblePlacement placement)
        {
            if (_messageBubble == null) return;

            // Center: giữa màn hình
            if (placement == BubblePlacement.Center || _currentHighlight == null)
            {
                _messageBubble.anchorMin = _messageBubble.anchorMax = new Vector2(0.5f, 0.5f);
                _messageBubble.anchoredPosition = Vector2.zero;
                return;
            }

            // Đặt bubble gần highlight target
            Vector3[] corners = new Vector3[4];
            _currentHighlight.GetWorldCorners(corners);
            Vector2 targetCenter = WorldToCanvasPoint((corners[0] + corners[2]) / 2f);
            Vector2 targetMin = WorldToCanvasPoint(corners[0]);
            Vector2 targetMax = WorldToCanvasPoint(corners[2]);

            float offset = 120f;
            Vector2 bubblePos = targetCenter;

            var effectivePlacement = placement;
            if (placement == BubblePlacement.Auto)
            {
                // Auto: nếu target ở nửa trên màn hình → bubble dưới, ngược lại
                effectivePlacement = targetCenter.y > 0 ? BubblePlacement.Below : BubblePlacement.Above;
            }

            switch (effectivePlacement)
            {
                case BubblePlacement.Above: bubblePos = new Vector2(targetCenter.x, targetMax.y + offset); break;
                case BubblePlacement.Below: bubblePos = new Vector2(targetCenter.x, targetMin.y - offset); break;
                case BubblePlacement.Left:  bubblePos = new Vector2(targetMin.x - offset, targetCenter.y); break;
                case BubblePlacement.Right: bubblePos = new Vector2(targetMax.x + offset, targetCenter.y); break;
            }

            _messageBubble.anchorMin = _messageBubble.anchorMax = new Vector2(0.5f, 0.5f);
            _messageBubble.anchoredPosition = bubblePos;
        }

        // ============================================================
        // INPUT
        // ============================================================
        public void OnPointerClick(PointerEventData eventData)
        {
            _tappedThisFrame = true;

            // Check tap có trúng vùng highlight không
            if (_currentHighlight != null && _allowClickThrough)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(
                    _currentHighlight, eventData.position, eventData.pressEventCamera))
                {
                    _highlightTappedThisFrame = true;
                }
            }
        }

        public bool WasTappedThisFrame() => _tappedThisFrame;
        public bool WasHighlightTappedThisFrame() => _highlightTappedThisFrame;

        // ============================================================
        // HELPERS
        // ============================================================
        private Vector2 WorldToCanvasPoint(Vector3 worldPoint)
        {
            // World corner (screen overlay canvas) → canvas local
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPoint);
            return ScreenToCanvasPoint(screenPoint);
        }

        private Vector2 ScreenToCanvasPoint(Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPoint,
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
                out Vector2 localPoint);
            return localPoint;
        }
    }
}
