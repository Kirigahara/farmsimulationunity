using UnityEngine;
using UnityEngine.UI;

namespace GameTemplate.Core.DevTools
{
    /// <summary>
    /// Hiện FPS trên màn hình khi test trên device thật.
    ///
    /// Tính năng:
    ///   - Hiện FPS hiện tại + smoothed (trung bình mượt)
    ///   - Color code: xanh ≥60, vàng 30-60, đỏ <30
    ///   - Toggle on/off bằng 3-finger tap hoặc keyboard (~)
    ///   - Optional: hiện thêm min/max FPS trong 1 giây
    ///   - Optional: hiện memory usage (MB)
    ///   - Position 4 góc màn hình (Inspector chọn)
    ///   - DontDestroyOnLoad - sống xuyên scene
    ///
    /// Cách dùng:
    ///   1. Tạo Canvas (overlay) trong scene Bootstrap
    ///   2. Tạo Text con (TextMeshPro hoặc legacy Text)
    ///   3. Add component FpsDisplay vào Canvas hoặc root → kéo Text vào field
    ///   4. Build dev và test trên device
    ///
    /// Hoặc auto-create: dùng [SerializeField] _autoCreateUI = true để tự tạo UI lúc Awake.
    /// </summary>
    public class FpsDisplay : MonoBehaviour
    {
        public enum DisplayCorner
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
        }

        [Header("UI Reference")]
        [Tooltip("Text component để hiện FPS. Null + AutoCreateUI=true → tự tạo.")]
        [SerializeField] private Text _fpsText;

        [Tooltip("Tự tạo Canvas + Text nếu _fpsText null. Cho prefab drag-drop dễ.")]
        [SerializeField] private bool _autoCreateUI = true;

        [SerializeField] private DisplayCorner _corner = DisplayCorner.TopRight;
        [SerializeField] private int _fontSize = 28;

        [Header("Display Options")]
        [SerializeField] private bool _showAverage = true;
        [SerializeField] private bool _showMinMax = false;
        [SerializeField] private bool _showMemory = false;

        [Header("Update Rate")]
        [Tooltip("Update text mỗi N giây. Quá nhanh sẽ flicker, quá chậm không real-time.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _updateInterval = 0.5f;

        [Header("Color Thresholds (FPS)")]
        [SerializeField] private int _goodFps = 60;
        [SerializeField] private int _warningFps = 30;
        [SerializeField] private Color _goodColor = new Color(0.2f, 1f, 0.2f, 1f);
        [SerializeField] private Color _warningColor = new Color(1f, 1f, 0.2f, 1f);
        [SerializeField] private Color _badColor = new Color(1f, 0.3f, 0.3f, 1f);

        [Header("Toggle")]
        [Tooltip("Phím để toggle (Editor + PC build). Default: ~")]
        [SerializeField] private KeyCode _toggleKey = KeyCode.BackQuote;

        [Tooltip("Mobile: tap N ngón cùng lúc để toggle.")]
        [SerializeField] private int _toggleFingerCount = 3;

        [Header("Lifecycle")]
        [SerializeField] private bool _dontDestroyOnLoad = true;

        // ===== Runtime state =====
        private float _accumDeltaTime;
        private int _frameCount;
        private float _timer;

        private float _currentFps;
        private float _smoothedFps;
        private float _minFps = float.MaxValue;
        private float _maxFps;

        // Smoothing - exponential moving average
        private const float SMOOTHING_FACTOR = 0.1f;

        private bool _isVisible = true;

        // ============================================================
        // LIFECYCLE
        // ============================================================
        private void Awake()
        {
            if (_dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

            if (_fpsText == null && _autoCreateUI)
            {
                CreateUI();
            }

            ApplyCornerAnchor();
        }

        private void Update()
        {
            HandleToggleInput();
            if (!_isVisible) return;

            AccumulateFps();

            _timer += Time.unscaledDeltaTime;
            if (_timer >= _updateInterval)
            {
                _timer = 0f;
                RefreshDisplay();
            }
        }

        // ============================================================
        // FPS CALCULATION
        // ============================================================
        private void AccumulateFps()
        {
            // Dùng unscaledDeltaTime để không bị ảnh hưởng bởi Time.timeScale
            // (game pause vẫn đếm FPS đúng)
            _accumDeltaTime += Time.unscaledDeltaTime;
            _frameCount++;

            // Tính FPS frame hiện tại
            _currentFps = 1f / Time.unscaledDeltaTime;

            // Smoothed FPS - exponential moving average (đỡ giật)
            _smoothedFps = _smoothedFps == 0
                ? _currentFps
                : Mathf.Lerp(_smoothedFps, _currentFps, SMOOTHING_FACTOR);

            // Track min/max
            if (_currentFps < _minFps) _minFps = _currentFps;
            if (_currentFps > _maxFps) _maxFps = _currentFps;
        }

        private void RefreshDisplay()
        {
            if (_fpsText == null) return;

            // FPS chính lấy từ smoothed (mượt) hoặc tính từ accumulated
            float displayFps = _showAverage
                ? _frameCount / _accumDeltaTime
                : _smoothedFps;

            // Build text
            var sb = new System.Text.StringBuilder(64);
            sb.Append("FPS: ").Append(Mathf.RoundToInt(displayFps));

            if (_showMinMax)
            {
                sb.Append("\nMin: ").Append(Mathf.RoundToInt(_minFps));
                sb.Append("  Max: ").Append(Mathf.RoundToInt(_maxFps));
            }

            if (_showMemory)
            {
                long memBytes = System.GC.GetTotalMemory(false);
                float memMB = memBytes / (1024f * 1024f);
                sb.Append("\nMem: ").Append(memMB.ToString("F1")).Append(" MB");
            }

            _fpsText.text = sb.ToString();
            _fpsText.color = GetColorForFps(displayFps);

            // Reset cho period tiếp theo
            _accumDeltaTime = 0f;
            _frameCount = 0;
            _minFps = float.MaxValue;
            _maxFps = 0f;
        }

        private Color GetColorForFps(float fps)
        {
            if (fps >= _goodFps) return _goodColor;
            if (fps >= _warningFps) return _warningColor;
            return _badColor;
        }

        // ============================================================
        // TOGGLE INPUT
        // ============================================================
        private void HandleToggleInput()
        {
            // PC/Editor: keyboard
            if (Input.GetKeyDown(_toggleKey))
            {
                Toggle();
                return;
            }

            // Mobile: N-finger tap
            if (_toggleFingerCount > 0 && Input.touchCount == _toggleFingerCount)
            {
                // Check tất cả ngón đều vừa bắt đầu chạm (TouchPhase.Began)
                bool allJustBegan = true;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    if (Input.GetTouch(i).phase != TouchPhase.Began)
                    {
                        allJustBegan = false;
                        break;
                    }
                }
                if (allJustBegan) Toggle();
            }
        }

        public void Toggle()
        {
            _isVisible = !_isVisible;
            if (_fpsText != null) _fpsText.enabled = _isVisible;
        }

        public void Show()
        {
            _isVisible = true;
            if (_fpsText != null) _fpsText.enabled = true;
        }

        public void Hide()
        {
            _isVisible = false;
            if (_fpsText != null) _fpsText.enabled = false;
        }

        // ============================================================
        // UI AUTO-CREATE
        // ============================================================
        private void CreateUI()
        {
            // Tạo Canvas overlay nếu chưa có sẵn parent canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("FpsDisplayCanvas");
                canvasGO.transform.SetParent(transform, false);
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999; // luôn trên cùng
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            // Tạo Text con
            var textGO = new GameObject("FpsText");
            textGO.transform.SetParent(canvas.transform, false);

            _fpsText = textGO.AddComponent<Text>();
            _fpsText.text = "FPS: --";
            _fpsText.fontSize = _fontSize;
            _fpsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _fpsText.alignment = TextAnchor.UpperLeft;
            _fpsText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _fpsText.verticalOverflow = VerticalWrapMode.Overflow;

            // Outline để dễ đọc trên mọi background
            var outline = textGO.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // RectTransform set theo corner
            var rt = textGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 100);
        }

        private void ApplyCornerAnchor()
        {
            if (_fpsText == null) return;
            var rt = _fpsText.rectTransform;

            // Margin từ edge màn hình
            const float margin = 20f;
            Vector2 anchor = Vector2.zero;
            Vector2 pivot = Vector2.zero;
            Vector2 pos = Vector2.zero;

            switch (_corner)
            {
                case DisplayCorner.TopLeft:
                    anchor = new Vector2(0, 1);
                    pivot = new Vector2(0, 1);
                    pos = new Vector2(margin, -margin);
                    _fpsText.alignment = TextAnchor.UpperLeft;
                    break;
                case DisplayCorner.TopRight:
                    anchor = new Vector2(1, 1);
                    pivot = new Vector2(1, 1);
                    pos = new Vector2(-margin, -margin);
                    _fpsText.alignment = TextAnchor.UpperRight;
                    break;
                case DisplayCorner.BottomLeft:
                    anchor = new Vector2(0, 0);
                    pivot = new Vector2(0, 0);
                    pos = new Vector2(margin, margin);
                    _fpsText.alignment = TextAnchor.LowerLeft;
                    break;
                case DisplayCorner.BottomRight:
                    anchor = new Vector2(1, 0);
                    pivot = new Vector2(1, 0);
                    pos = new Vector2(-margin, margin);
                    _fpsText.alignment = TextAnchor.LowerRight;
                    break;
            }

            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && _fpsText != null) ApplyCornerAnchor();
        }
#endif
    }
}
