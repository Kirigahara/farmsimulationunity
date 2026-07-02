using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using GameTemplate.Core.Events;

namespace GameTemplate.Gameplay
{
    public struct CameraAlign : IGameEvent
    {
        public Vector3 _Target;
        public Action _AlignComplete;
    }

    /// <summary>
    /// Camera controller — chỉ điều phối CameraDragInput và CameraRig.
    /// SRP: MonoBehaviour này không xử lý input hay math, chỉ kết nối các class lại.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Boundary")]
        [SerializeField] private bool _useBoundary = true;
        [SerializeField] private Vector2 _boundaryCenter = Vector2.zero;
        [SerializeField] private Vector2 _boundarySize = new Vector2(20f, 20f);

        [Header("Drag")]
        [SerializeField] private float _dragSpeed = 1f;

        [Header("Align")]
        [SerializeField] private float _alignSpeed = 5f;

        // ---------------------------------------------------------------
        private CameraDragInput _dragInput;
        private CameraRig _rig;
        private CancellationTokenSource _alignCts;
        private Action _alignComplete;

        // ---------------------------------------------------------------

        private void Awake()
        {
            var cam = GetComponent<Camera>() ?? Camera.main;

            _rig = new CameraRig(transform, cam, _useBoundary, _boundaryCenter, _boundarySize);

            _dragInput = new CameraDragInput();
            _dragInput.OnDragBegan += OnDragBegan;
            _dragInput.OnDrag      += OnDrag;

            EventBus.Subscribe<CameraAlign>(OnAlignCamera);
        }

        private void Update()
        {
            _dragInput.Tick();
        }

        private void OnDestroy()
        {
            _dragInput.OnDragBegan -= OnDragBegan;
            _dragInput.OnDrag      -= OnDrag;

            EventBus.Unsubscribe<CameraAlign>(OnAlignCamera);

            _alignCts?.Cancel();
            _alignCts?.Dispose();
        }

        // ---------------------------------------------------------------
        // Drag callbacks
        // ---------------------------------------------------------------

        private void OnDragBegan()
        {
            _alignCts?.Cancel();
        }

        private void OnDrag(Vector2 screenDelta)
        {
            _rig.MoveByScreenDelta(screenDelta, _dragSpeed);
        }

        // ---------------------------------------------------------------
        // Align
        // ---------------------------------------------------------------

        private void OnAlignCamera(CameraAlign e)
        {
            _alignComplete = e._AlignComplete;
            AlignToTarget(e._Target);
        }

        public void AlignToTarget(Vector3 targetPosition)
        {
            _alignCts?.Cancel();
            _alignCts?.Dispose();
            _alignCts = new CancellationTokenSource();
            _ = AlignAsync(targetPosition, _alignCts.Token);
        }

        private async Task AlignAsync(Vector3 targetPosition, CancellationToken ct)
        {
            Vector3 destination = _rig.GetAlignDestination(targetPosition);

            while (!ct.IsCancellationRequested)
            {
                Vector3 next = Vector3.MoveTowards(
                    _rig.Position,
                    destination,
                    _alignSpeed * Time.deltaTime
                );
                _rig.MoveTo(next);

                if (Vector3.Distance(_rig.Position, destination) < 0.01f)
                {
                    _rig.MoveTo(destination);
                    break;
                }

                await Task.Yield();
            }

            if (!ct.IsCancellationRequested)
                _alignComplete?.Invoke();
        }

        // ---------------------------------------------------------------
        // Gizmos
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
