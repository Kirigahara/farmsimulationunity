using UnityEngine;

namespace GameTemplate.Gameplay
{
    /// <summary>
    /// Chỉ xử lý di chuyển camera: MoveByDelta, MoveTo, Boundary.
    /// Không biết gì về input hay async.
    /// </summary>
    public class CameraRig
    {
        private readonly Transform _transform;
        private readonly Camera _camera;
        private readonly float _fixedY;

        // Boundary
        private bool _useBoundary;
        private Vector2 _boundaryCenter;
        private Vector2 _boundarySize;

        // Cache mặt phẳng XZ — không tạo lại mỗi frame
        private readonly Plane _groundPlane = new Plane(Vector3.up, Vector3.zero);

        public Vector3 Position => _transform.position;

        public CameraRig(Transform transform, Camera camera, bool useBoundary, Vector2 boundaryCenter, Vector2 boundarySize)
        {
            _transform     = transform;
            _camera        = camera;
            _fixedY        = transform.position.y;
            _useBoundary   = useBoundary;
            _boundaryCenter = boundaryCenter;
            _boundarySize   = boundarySize;
        }

        /// <summary>Di chuyển camera theo screen delta (từ CameraDragInput).</summary>
        public void MoveByScreenDelta(Vector2 screenDelta, float dragSpeed)
        {
            Vector3 center = ScreenToGround(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
            Vector3 offset = ScreenToGround(new Vector3(Screen.width * 0.5f + screenDelta.x,
                                                        Screen.height * 0.5f + screenDelta.y));
            Vector3 worldDelta = (offset - center) * dragSpeed;
            ApplyPosition(_transform.position - worldDelta);
        }

        /// <summary>Di chuyển camera đến vị trí cụ thể (dùng cho align).</summary>
        public void MoveTo(Vector3 position)
        {
            ApplyPosition(position);
        }

        /// <summary>
        /// Tính destination để target xuất hiện ở tâm màn hình.
        /// </summary>
        public Vector3 GetAlignDestination(Vector3 targetPosition)
        {
            Vector3 screenCenter = ScreenToGround(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
            Vector3 cameraOffset = _transform.position - screenCenter;

            Vector3 destination = new Vector3(
                targetPosition.x + cameraOffset.x,
                _fixedY,
                targetPosition.z + cameraOffset.z
            );
            return ClampToBoundary(destination);
        }

        // ---------------------------------------------------------------

        private void ApplyPosition(Vector3 position)
        {
            position.y = _fixedY;
            _transform.position = ClampToBoundary(position);
        }

        private Vector3 ClampToBoundary(Vector3 position)
        {
            if (!_useBoundary) return position;

            float halfX = _boundarySize.x * 0.5f;
            float halfZ = _boundarySize.y * 0.5f;

            position.x = Mathf.Clamp(position.x, _boundaryCenter.x - halfX, _boundaryCenter.x + halfX);
            position.z = Mathf.Clamp(position.z, _boundaryCenter.y - halfZ, _boundaryCenter.y + halfZ);
            return position;
        }

        public Vector3 ScreenToGround(Vector3 screenPos)
        {
            Ray ray = _camera.ScreenPointToRay(screenPos);
            if (_groundPlane.Raycast(ray, out float distance))
                return ray.GetPoint(distance);
            return _transform.position;
        }
    }
}
