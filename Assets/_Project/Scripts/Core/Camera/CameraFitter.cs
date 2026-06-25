using UnityEngine;

namespace GameTemplate.Core.Camera
{
    /// <summary>
    /// Camera fit strategy - quyết định cách camera show gameplay area trên màn hình.
    /// </summary>
    public enum CameraFitMode
    {
        /// <summary>
        /// Cố định chiều ngang (DesignWidth) - máy aspect khác thấy nhiều/ít chiều dọc.
        /// Phù hợp: portrait game, side-scroller, runner.
        /// </summary>
        FitWidth,

        /// <summary>
        /// Cố định chiều dọc (DesignHeight) - máy aspect khác thấy nhiều/ít chiều ngang.
        /// Phù hợp: landscape game, vertical shooter, top-down.
        /// </summary>
        FitHeight,

        /// <summary>
        /// Auto detect: Portrait → FitWidth, Landscape → FitHeight.
        /// Phù hợp: game support cả 2 orientation, hoặc dev chưa quyết.
        /// </summary>
        AutoByOrientation,
    }

    /// <summary>
    /// Auto-fit Camera (Orthographic 2D hoặc Perspective 3D) vào design area trên mọi device aspect.
    ///
    /// Triết lý: KHÔNG scale game object - object luôn ở scale 1 (physics, animation, batching đều OK).
    /// Camera tự điều chỉnh để show đúng "design area" mà bạn muốn guarantee visible.
    ///
    /// Cách dùng:
    ///   1. Gắn component này lên Camera GameObject (Main Camera)
    ///   2. Set DesignWidth + DesignHeight = vùng game cần luôn visible (world units)
    ///   3. Chọn FitMode (hoặc để AutoByOrientation cho auto)
    ///   4. Khi build, mọi device aspect đều thấy ít nhất design area
    ///
    /// Lưu ý:
    ///   - Object tràn ra ngoài design area = "extra space" - có thể thấy ở máy aspect khác
    ///   - Background nên vẽ TO HƠN design area để fill extra space (tránh viền đen)
    ///   - UI quan trọng đặt trong design area, không spawn enemy ở extra space
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    [DisallowMultipleComponent]
    public class CameraFitter : MonoBehaviour
    {
        [Header("Design Area (world units)")]
        [Tooltip("Chiều ngang vùng game luôn visible.")]
        [SerializeField] private float _designWidth = 9f;
        [Tooltip("Chiều dọc vùng game luôn visible.")]
        [SerializeField] private float _designHeight = 16f;

        [Header("Fit Mode")]
        [SerializeField] private CameraFitMode _mode = CameraFitMode.AutoByOrientation;

        [Header("Debug")]
        [Tooltip("Log mỗi lần re-fit (dev only).")]
        [SerializeField] private bool _logChanges = false;
        [Tooltip("Vẽ gizmo design area trong Scene view.")]
        [SerializeField] private bool _drawGizmo = true;

        private UnityEngine.Camera _camera;
        private Vector2Int _lastScreenSize = Vector2Int.zero;
        private CameraFitMode _lastEffectiveMode;

        // ===== Public read-only =====
        public UnityEngine.Camera Camera => _camera;
        public float DesignWidth => _designWidth;
        public float DesignHeight => _designHeight;
        public CameraFitMode Mode => _mode;

        /// <summary>Effective mode đang áp dụng (resolve AutoByOrientation thành Width/Height).</summary>
        public CameraFitMode EffectiveMode { get; private set; }

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            ApplyFit();
        }

        private void Update()
        {
            // Detect screen size change (xoay device, split screen, foldable)
            if (Screen.width != _lastScreenSize.x || Screen.height != _lastScreenSize.y)
                ApplyFit();
        }

        private void ApplyFit()
        {
            if (_camera == null) _camera = GetComponent<UnityEngine.Camera>();
            if (Screen.width <= 0 || Screen.height <= 0) return;

            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            // Resolve AutoByOrientation thành mode cụ thể
            EffectiveMode = ResolveEffectiveMode();
            float screenAspect = (float)Screen.width / Screen.height;

            if (_camera.orthographic)
            {
                ApplyOrthographic(screenAspect);
            }
            else
            {
                ApplyPerspective(screenAspect);
            }

            if (_logChanges)
            {
                Debug.Log(
                    $"[CameraFitter] Applied: screen={Screen.width}x{Screen.height} " +
                    $"(aspect={screenAspect:F2}), mode={EffectiveMode}, " +
                    $"design={_designWidth}x{_designHeight}",
                    this);
            }
        }

        private CameraFitMode ResolveEffectiveMode()
        {
            if (_mode != CameraFitMode.AutoByOrientation) return _mode;

            // Auto: dựa vào aspect màn hình hiện tại
            // - aspect < 1 (portrait, vd 9:16) → FitWidth (đảm bảo chiều ngang)
            // - aspect >= 1 (landscape, vd 16:9) → FitHeight (đảm bảo chiều dọc)
            float aspect = (float)Screen.width / Screen.height;
            return aspect < 1f ? CameraFitMode.FitWidth : CameraFitMode.FitHeight;
        }

        // ============================================================
        // ORTHOGRAPHIC (2D camera)
        // ============================================================
        private void ApplyOrthographic(float screenAspect)
        {
            // Camera.orthographicSize = NỬA chiều cao view trong world units
            // → Chiều cao view = orthoSize * 2
            // → Chiều ngang view = orthoSize * 2 * screenAspect

            float orthoSize;

            if (EffectiveMode == CameraFitMode.FitWidth)
            {
                // Muốn chiều ngang luôn = _designWidth
                // _designWidth = orthoSize * 2 * screenAspect
                // → orthoSize = _designWidth / (2 * screenAspect)
                orthoSize = _designWidth / (2f * screenAspect);
            }
            else // FitHeight
            {
                // Muốn chiều dọc luôn = _designHeight
                // _designHeight = orthoSize * 2
                // → orthoSize = _designHeight / 2
                orthoSize = _designHeight / 2f;
            }

            _camera.orthographicSize = orthoSize;
        }

        // ============================================================
        // PERSPECTIVE (3D camera)
        // ============================================================
        private void ApplyPerspective(float screenAspect)
        {
            // Perspective: dùng FieldOfView (góc dọc) + distance từ camera tới target plane
            // Giả sử design area nằm ở mặt phẳng z = 0, camera ở z = -distance
            // Distance = camera position magnitude (assume looking forward)

            // Camera fov dọc xác định chiều cao visible ở 1 distance:
            // visibleHeight = 2 * distance * tan(fov / 2)
            // visibleWidth = visibleHeight * screenAspect

            float distance = Mathf.Abs(transform.position.z);
            if (distance < 0.01f) distance = 10f; // fallback nếu camera ở origin

            float targetHeight, targetWidth;

            if (EffectiveMode == CameraFitMode.FitWidth)
            {
                targetWidth = _designWidth;
                targetHeight = targetWidth / screenAspect;
            }
            else // FitHeight
            {
                targetHeight = _designHeight;
                targetWidth = targetHeight * screenAspect;
            }

            // Tính fov dọc để chiều cao plane = targetHeight tại distance này
            // fov = 2 * atan(targetHeight / (2 * distance))
            float fovRadians = 2f * Mathf.Atan(targetHeight / (2f * distance));
            _camera.fieldOfView = fovRadians * Mathf.Rad2Deg;
        }

        // ============================================================
        // EDITOR GIZMO - vẽ design area trong Scene view
        // ============================================================
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_drawGizmo) return;

            Gizmos.color = Color.cyan;
            // Vẽ rectangle design area centered tại transform position (camera position)
            // Chỉ work tốt cho orthographic - perspective phải tính theo distance
            var cam = GetComponent<UnityEngine.Camera>();
            if (cam == null) return;

            if (cam.orthographic)
            {
                Vector3 center = new Vector3(
                    transform.position.x,
                    transform.position.y,
                    transform.position.z + cam.nearClipPlane + 1f
                );
                Gizmos.DrawWireCube(center, new Vector3(_designWidth, _designHeight, 0.1f));
            }
            else
            {
                // Perspective: vẽ tại target distance (giả sử z=0)
                float distance = Mathf.Abs(transform.position.z);
                if (distance < 0.01f) distance = 10f;
                Vector3 center = transform.position + transform.forward * distance;
                Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(_designWidth, _designHeight, 0.1f));
                Gizmos.matrix = Matrix4x4.identity;
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying && _camera != null) ApplyFit();
        }
#endif
    }
}
