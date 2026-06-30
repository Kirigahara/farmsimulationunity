using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using GameTemplate.Core.Patterns.Async;
using GameTemplate.Core.Events;
using GameTemplate.Core.DI;
using System;

namespace GameTemplate.Gameplay
{
    public struct CameraAlign : IGameEvent 
    { 
        public Vector3 _Target;
        public Action _AlignComplete;
    }

    /// <summary>
    /// Camera controller cho game mobile 3D màn hình dọc.
    ///
    /// Tính năng:
    ///   - Kéo 1 ngón tay để di chuyển camera trên mặt phẳng XZ
    ///   - Giới hạn vùng di chuyển bằng Bounds
    ///   - AlignToTarget: tự động di chuyển camera để target vào tâm màn hình
    ///   - Camera giữ nguyên Y (không thay đổi khoảng cách với mặt phẳng XZ)
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Boundary — giới hạn vùng camera có thể di chuyển")]
        [Tooltip("Bật/tắt giới hạn vùng di chuyển.")]
        [SerializeField] private bool _useBoundary = true;

        [Tooltip("Tâm vùng di chuyển (chỉ dùng X và Z).")]
        [SerializeField] private Vector2 _boundaryCenter = Vector2.zero;

        [Tooltip("Kích thước vùng di chuyển (X = chiều rộng, Y = chiều sâu).")]
        [SerializeField] private Vector2 _boundarySize = new Vector2(20f, 20f);

        [Header("Drag")]
        [Tooltip("Hệ số tốc độ kéo — tăng nếu camera di chuyển quá chậm.")]
        [SerializeField] private float _dragSpeed = 1f;

        [Header("Align")]
        [Tooltip("Tốc độ di chuyển khi gọi AlignToTarget.")]
        [SerializeField] private float _alignSpeed = 5f;

        // ---------------------------------------------------------------
        private Camera _camera;
        private Vector3 _fixedY;           // giữ Y cố định
        private Vector3 _dragOrigin;       // điểm chạm trên mặt phẳng XZ lúc bắt đầu kéo
        private bool _isDragging;

        private CancellationTokenSource _alignCts;

        // ---------------------------------------------------------------
        private Vector2 _lastMousePos;

        Action _AlignComplete;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
                _camera = Camera.main;

            _fixedY = transform.position;

            EventBus.Subscribe<CameraAlign>(OnAlignCamera);
        }

        private void Update()
        {
            HandleDrag();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CameraAlign>(OnAlignCamera);

            _alignCts?.Cancel();
            _alignCts?.Dispose();
        }

        // ---------------------------------------------------------------
        // Drag
        // ---------------------------------------------------------------

        private void HandleDrag()
        {
#if UNITY_EDITOR
            HandleMouseDrag();
#else
            HandleTouchDrag();
#endif
        }

        private void HandleTouchDrag()
        {
            if (Input.touchCount != 1)
            {
                _isDragging = false;
                return;
            }

            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                _isDragging = true;
                _lastMousePos = touch.position;
                _alignCts?.Cancel();
            }
            else if (touch.phase == TouchPhase.Moved && _isDragging)
            {
                Vector2 screenDelta = touch.deltaPosition;
                _lastMousePos = touch.position;

                Vector3 worldDelta = ScreenDeltaToWorld(screenDelta);
                MoveCamera(transform.position - worldDelta);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                _isDragging = false;
            }
        }

        private void HandleMouseDrag()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _isDragging = true;
                _lastMousePos = Input.mousePosition;
                _alignCts?.Cancel();
            }
            else if (Input.GetMouseButton(0) && _isDragging)
            {
                Vector2 currentPos = Input.mousePosition;
                Vector2 screenDelta = currentPos - _lastMousePos;
                _lastMousePos = currentPos;

                // Chuyển screen delta sang world delta
                Vector3 worldDelta = ScreenDeltaToWorld(screenDelta);
                MoveCamera(transform.position - worldDelta);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
            }
        }

        // ---------------------------------------------------------------
        // Align to target
        // ---------------------------------------------------------------

        /// <summary>
        /// Di chuyển camera để target xuất hiện ở tâm màn hình.
        /// Camera chỉ di chuyển trên mặt phẳng XZ, Y giữ nguyên.
        /// </summary>
        public void AlignToTarget(Vector3 targetPosition)
        {
            // Cancel align cũ nếu đang chạy
            _alignCts?.Cancel();
            _alignCts?.Dispose();
            _alignCts = new CancellationTokenSource();

            _ = AlignAsync(targetPosition, _alignCts.Token);
        }

        private async Task AlignAsync(Vector3 targetPosition, CancellationToken ct)
        {
            // Tính offset từ camera đến điểm nó đang nhìn trên mặt phẳng XZ
            // Để target vào giữa màn hình, camera phải dịch đúng offset này
            Vector3 screenCenter = GetWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
            Vector3 cameraOffset = transform.position - screenCenter;

            // Destination = target + offset → target sẽ nằm đúng tâm màn hình
            Vector3 destination = new Vector3(
                targetPosition.x + cameraOffset.x,
                transform.position.y,   // giữ Y
                targetPosition.z + cameraOffset.z
            );

            destination = ClampToBoundary(destination);

            while (!ct.IsCancellationRequested)
            {
                Vector3 next = Vector3.MoveTowards(
                    transform.position,
                    destination,
                    _alignSpeed * Time.deltaTime
                );
                transform.position = next;

                if (Vector3.Distance(transform.position, destination) < 0.01f)
                {
                    transform.position = destination;
                    break;
                }

                await Task.Yield();
            }

            if (!ct.IsCancellationRequested)
                _AlignComplete?.Invoke();

            //_AlignComplete?.Invoke();
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        /// <summary>Di chuyển camera đến vị trí mới, giữ Y và clamp vào boundary.</summary>
        private void MoveCamera(Vector3 targetPosition)
        {
            targetPosition.y = _fixedY.y;
            transform.position = ClampToBoundary(targetPosition);
        }

        /// <summary>Clamp vị trí vào trong boundary (XZ).</summary>
        private Vector3 ClampToBoundary(Vector3 position)
        {
            if (!_useBoundary) return position;

            float halfX = _boundarySize.x * 0.5f;
            float halfZ = _boundarySize.y * 0.5f;

            position.x = Mathf.Clamp(position.x, _boundaryCenter.x - halfX, _boundaryCenter.x + halfX);
            position.z = Mathf.Clamp(position.z, _boundaryCenter.y - halfZ, _boundaryCenter.y + halfZ);
            return position;
        }

        /// <summary>
        /// Chuyển tọa độ màn hình thành tọa độ thế giới trên mặt phẳng XZ (Y = camera.Y).
        /// </summary>
        private Vector3 GetWorldPoint(Vector3 screenPosition)
        {
            Ray ray = _camera.ScreenPointToRay(screenPosition);

            // Mặt phẳng XZ tại Y = 0 (mặt đất thực tế)
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float distance))
                return ray.GetPoint(distance);

            return transform.position;
        }

        /// <summary>
        /// Chuyển delta pixel màn hình thành delta world space trên mặt phẳng XZ.
        /// Tính đến góc camera nên drag luôn đúng hướng bất kể rotation.
        /// </summary>
        private Vector3 ScreenDeltaToWorld(Vector2 screenDelta)
        {
            // Lấy 2 điểm world từ tâm màn hình và tâm + delta
            Vector3 center = GetWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
            Vector3 offset = GetWorldPoint(new Vector3(Screen.width * 0.5f + screenDelta.x,
                                                       Screen.height * 0.5f + screenDelta.y));
            return (offset - center) * _dragSpeed;
        }

        public void OnAlignCamera(CameraAlign e)
        {
            _AlignComplete = e._AlignComplete;
            AlignToTarget(e._Target);
        }

        // ---------------------------------------------------------------
        // Gizmos — visualize boundary trong Editor
        // ---------------------------------------------------------------

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_useBoundary) return;

            Gizmos.color = Color.cyan;
            Vector3 center = new Vector3(_boundaryCenter.x, transform.position.y, _boundaryCenter.y);
            Vector3 size   = new Vector3(_boundarySize.x, 0.1f, _boundarySize.y);
            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}
