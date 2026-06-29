using GameTemplate.Core.Patterns.Async;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;

namespace GameTemplate.Gameplay
{
    public static class PathSmoother
    {
        public static void FindPath(
            Vector3 CurrentPosition,
            PathNode CurrentNode,
            PathNode FinishNode,
            Action<List<Vector3>> SetPathCallBack)
        {
            bool endpath = false;
            List<Vector3> listNode = new List<Vector3>();
            listNode.Add(CurrentPosition);

            PathNode startNode = CurrentNode;
            Vector3 pos = CurrentPosition;

            _ = new Func<Task>(async () =>
            {
                while (!endpath)
                {
                    (endpath, startNode, pos) = startNode.GetConnectNode(FinishNode);

                    listNode.Add(pos);

                    await AsyncOp.Delay(Time.deltaTime);
                }

                SetPathCallBack.Invoke(Smooth(listNode));
            })();
        }

        /// <summary>
        /// Phase 3: Nhận list Vector3 từ phase 2, trả về đường cong mịn đã re-sample đều.
        /// Di chuyển trên mặt phẳng ZX — Y giữ nguyên từ node gốc.
        /// </summary>
        /// <param name="waypoints">List node từ phase 2.</param>
        /// <param name="pointSpacing">Khoảng cách giữa các điểm output (tune theo speed).</param>
        /// <param name="alpha">0.5 = Centripetal — recommended cho pathfinding.</param>
        public static List<Vector3> Smooth(List<Vector3> waypoints, float pointSpacing = 0.1f, float alpha = 0.5f)
        {
            if (waypoints == null || waypoints.Count < 2) return waypoints;

            // Thêm ghost point 2 đầu
            var pts = new List<Vector3>();
            pts.Add(waypoints[0] + (waypoints[0] - waypoints[1]));
            pts.AddRange(waypoints);
            pts.Add(waypoints[^1] + (waypoints[^1] - waypoints[^2]));

            // Sample dày Catmull-Rom (chưa đều)
            var rawCurve = new List<Vector3>();
            for (int i = 1; i < pts.Count - 2; i++)
            {
                int sampleCount = Mathf.Max(10, Mathf.RoundToInt(
                    DistanceZX(pts[i], pts[i + 1]) / pointSpacing * 5f));

                for (int s = 0; s <= sampleCount; s++)
                {
                    float t = s / (float)sampleCount;
                    rawCurve.Add(EvaluateZX(pts[i - 1], pts[i], pts[i + 1], pts[i + 2], t, alpha));
                }
            }

            // Re-sample đều theo arc-length trên ZX
            return ResampleEvenZX(rawCurve, pointSpacing);
        }

        // ---------------------------------------------------------------

        private static Vector3 EvaluateZX(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, float alpha)
        {
            float GetT(float t0, Vector3 a, Vector3 b)
            {
                float dx = b.x - a.x, dz = b.z - a.z;
                return Mathf.Pow(dx * dx + dz * dz, alpha * 0.5f) + t0;
            }

            float t0 = 0f;
            float t1 = GetT(t0, p0, p1);
            float t2 = GetT(t1, p1, p2);
            float t3 = GetT(t2, p2, p3);

            t = Mathf.Lerp(t1, t2, t);

            Vector3 A1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
            Vector3 A2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2;
            Vector3 A3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3;

            Vector3 B1 = (t2 - t) / (t2 - t0) * A1 + (t - t0) / (t2 - t0) * A2;
            Vector3 B2 = (t3 - t) / (t3 - t1) * A2 + (t - t1) / (t3 - t1) * A3;

            Vector3 result = (t2 - t) / (t2 - t1) * B1 + (t - t1) / (t2 - t1) * B2;

            // Y giữ nguyên từ p1 (node gốc), không nội suy
            result.y = p1.y;
            return result;
        }

        private static float DistanceZX(Vector3 a, Vector3 b)
        {
            float dx = b.x - a.x, dz = b.z - a.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static List<Vector3> ResampleEvenZX(List<Vector3> curve, float spacing)
        {
            var result = new List<Vector3> { curve[0] };
            float accumulated = 0f;

            for (int i = 1; i < curve.Count; i++)
            {
                float segLen = DistanceZX(curve[i - 1], curve[i]);
                accumulated += segLen;

                while (accumulated >= spacing)
                {
                    accumulated -= spacing;
                    float t = 1f - accumulated / segLen;
                    result.Add(Vector3.Lerp(curve[i - 1], curve[i], t));
                }
            }

            // Đảm bảo điểm cuối luôn có trong path
            result.Add(curve[^1]);
            return result;
        }
    }
}
