using System.Collections.Generic;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class PathFollower
    {
        private List<Vector3> _path;
        private int _currentIndex;
        private float _stoppingDistance;

        public bool IsFinished { get; private set; }
        public Vector3 CurrentTarget => _path[_currentIndex];
        public Vector3 MoveDirection { get; private set; } // hướng di chuyển frame hiện tại

        public void SetPath(List<Vector3> path, float stoppingDistance = 0.1f)
        {
            _path = path;
            _currentIndex = 0;
            IsFinished = path == null || path.Count == 0;
            _stoppingDistance = stoppingDistance;
            MoveDirection = Vector3.forward; // reset về forward khi nhận path mới
        }

        public Vector3 Tick(Vector3 currentPosition, float speed)
        {
            if (IsFinished) return currentPosition;

            float dist = Vector3.Distance(currentPosition, _path[_currentIndex]);
            if (dist <= _stoppingDistance)
            {
                _currentIndex++;
                if (_currentIndex >= _path.Count)
                {
                    IsFinished = true;
                    return currentPosition;
                }
            }

            Vector3 nextPosition = Vector3.MoveTowards(currentPosition, _path[_currentIndex], speed * Time.deltaTime);

            // Tính direction trên mặt phẳng ZX, bỏ qua Y
            Vector3 dir = _path[_currentIndex] - currentPosition;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f) // tránh direction = zero khi đứng yên
                MoveDirection = dir.normalized;

            return nextPosition;
        }
    }
}
