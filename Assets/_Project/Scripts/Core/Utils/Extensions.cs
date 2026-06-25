using UnityEngine;

namespace GameTemplate.Core.Utils
{
    /// <summary>
    /// Extension methods hay dùng. Bỏ tốn ít LOC khi code gameplay.
    /// </summary>
    public static class Extensions
    {
        // ----- Transform -----
        public static void ResetLocal(this Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        public static void DestroyChildren(this Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                Object.Destroy(t.GetChild(i).gameObject);
        }

        // ----- GameObject -----
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            if (!go.TryGetComponent<T>(out var c))
                c = go.AddComponent<T>();
            return c;
        }

        // ----- Vector -----
        public static Vector3 WithX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
        public static Vector3 WithZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);

        // ----- Float -----
        public static bool Approximately(this float a, float b, float epsilon = 0.0001f)
            => Mathf.Abs(a - b) < epsilon;
    }
}
