using System.Collections.Generic;
using UnityEngine;

public class GrassArea : MonoBehaviour
{
    [Header("Grass Settings")]
    public Mesh grassMesh;
    public Material grassMaterial;

    [Header("Spawn Settings")]
    public int grassCount = 5000;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;

    [Header("Bounds (Frustum Culling)")]
    // Size của toàn bộ khu vực cỏ — dùng để cull cả chunk khi ra khỏi camera
    public Vector3 areaBoundsSize = new Vector3(50f, 5f, 50f);

    [Header("Material")]
    public Material _AreaMaterial;

    // ── Runtime ───────────────────────────────────────────────────────────
    private List<Matrix4x4[]> _chunks = new List<Matrix4x4[]>();
    private Bounds _areaBounds;
    private Camera _mainCamera;

    private const int BATCH_SIZE = 1023; // Giới hạn của DrawMeshInstanced

    // ─────────────────────────────────────────────────────────────────────
    //  Public API — gọi từ editor button hoặc từ code khác
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawn cỏ random trong danh sách polygon points (world space).
    /// Gọi hàm này sau khi set grassCount, minScale, maxScale.
    /// </summary>
    public void Spawn(Vector3[] polygonPoints)
    {
        if (grassMesh == null || grassMaterial == null)
        {
            Debug.LogError("[GrassArea] Thiếu Mesh hoặc Material!", this);
            return;
        }

        var allMatrices = GenerateMatrices(polygonPoints);
        BuildChunks(allMatrices);

        // Tính Bounds bao toàn bộ khu vực để dùng cho frustum culling
        _areaBounds = new Bounds(transform.position, areaBoundsSize);

        Debug.Log($"[GrassArea] Spawned {allMatrices.Count} instances in {_chunks.Count} chunks.");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        _mainCamera = Camera.main;

        List<Vector3> limitPoint = new List<Vector3>();
        for (int i = 0; i < this.transform.childCount; i++)
        {
            limitPoint.Add(this.transform.GetChild(i).position);
        }
        Spawn(limitPoint.ToArray());

        Mesh mesh = CreatePolygonMesh(limitPoint.ToArray());

        this.gameObject.AddComponent<MeshFilter>().mesh = mesh;
        this.gameObject.AddComponent<MeshRenderer>().material = _AreaMaterial;
    }

    void Update()
    {
        if (_chunks.Count == 0) return;

        // ── Frustum Culling ở cấp độ khu vực ────────────────────────────
        // Nếu toàn bộ area nằm ngoài camera thì skip tất cả draw call
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
        if (!GeometryUtility.TestPlanesAABB(frustumPlanes, _areaBounds)) return;

        // ── Draw ─────────────────────────────────────────────────────────
        foreach (var chunk in _chunks)
        {
            Graphics.DrawMeshInstanced(
                grassMesh,
                0,
                grassMaterial,
                chunk,
                chunk.Length,
                null,                              // MaterialPropertyBlock
                UnityEngine.Rendering.ShadowCastingMode.Off,  // castShadows
                false
            );
        }
    }

    private Mesh CreatePolygonMesh(Vector3[] polygon, float yOffset = 0.01f)
    {
        Vector3 center = Vector3.zero;
        foreach (var p in polygon)
            center += p;
        center /= polygon.Length;
        center.y = yOffset;

        Vector3[] vertices = new Vector3[polygon.Length + 1];
        vertices[0] = transform.InverseTransformPoint(center);
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector3 worldPoint = new Vector3(polygon[i].x, yOffset, polygon[i].z);
            vertices[i + 1] = transform.InverseTransformPoint(worldPoint);
        }

        int[] triangles = new int[polygon.Length * 3];
        for (int i = 0; i < polygon.Length; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % polygon.Length + 1;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────

    private List<Matrix4x4> GenerateMatrices(Vector3[] polygon)
    {
        var matrices = new List<Matrix4x4>(grassCount);

        // Tính bounding rect của polygon để random nhanh hơn
        Bounds polyBounds = GetPolygonBounds(polygon);
        int safetyLimit = grassCount * 10; // tránh vòng lặp vô tận nếu polygon nhỏ
        int attempts = 0;

        while (matrices.Count < grassCount && attempts < safetyLimit)
        {
            attempts++;

            // Random điểm trong bounding rect
            //float x = Random.Range(polyBounds.min.x, polyBounds.max.x);
            //float z = Random.Range(polyBounds.min.z, polyBounds.max.z);

            float x = 0;
            float z = 0;

            (x,z) = StaticFunction.RandomPointOnPolygon(polygon);

            Vector3 candidate = new Vector3(x, transform.position.y, z);

            // Kiểm tra điểm có nằm trong polygon không
            //if (!IsPointInPolygon(candidate, polygon)) continue;

            // Random rotation quanh Y
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // Random scale đều 3 trục
            float s = StaticFunction.RandomFloat(minScale, maxScale);
            Vector3 scale = new Vector3(s, s, s);

            matrices.Add(Matrix4x4.TRS(candidate, rot, scale));
        }

        if (matrices.Count < grassCount)
            Debug.LogWarning($"[GrassArea] Chỉ spawn được {matrices.Count}/{grassCount} — polygon có thể quá nhỏ.");

        return matrices;
    }


    private void BuildChunks(List<Matrix4x4> allMatrices)
    {
        _chunks.Clear();
        int total = allMatrices.Count;

        for (int i = 0; i < total; i += BATCH_SIZE)
        {
            int size = Mathf.Min(BATCH_SIZE, total - i);
            var chunk = new Matrix4x4[size];
            for (int j = 0; j < size; j++)
                chunk[j] = allMatrices[i + j];
            _chunks.Add(chunk);
        }
    }

    /// <summary>
    /// Point-in-polygon (ray casting algorithm) — hoạt động trên mặt phẳng XZ.
    /// </summary>
    private bool IsPointInPolygon(Vector3 point, Vector3[] polygon)
    {
        int n = polygon.Length;
        bool inside = false;
        int j = n - 1;

        for (int i = 0; i < n; i++)
        {
            float xi = polygon[i].x, zi = polygon[i].z;
            float xj = polygon[j].x, zj = polygon[j].z;

            bool intersect = ((zi > point.z) != (zj > point.z))
                && (point.x < (xj - xi) * (point.z - zi) / (zj - zi) + xi);

            if (intersect) inside = !inside;
            j = i;
        }

        return inside;
    }

    private Bounds GetPolygonBounds(Vector3[] polygon)
    {
        Vector3 min = polygon[0];
        Vector3 max = polygon[0];

        foreach (var p in polygon)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        Vector3 center = (min + max) / 2f;
        Vector3 size = max - min;
        return new Bounds(center, size);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Gizmos — visualize khu vực trong Scene view
    // ─────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    [Header("Debug")]
    public Vector3[] debugPolygon; // set trong inspector để preview

    [ContextMenu(nameof(OnDrawGizmosSelected))]
    public void OnDrawGizmosSelected()
    {
        if (debugPolygon == null || debugPolygon.Length < 3) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < debugPolygon.Length; i++)
        {
            Vector3 a = debugPolygon[i];
            Vector3 b = debugPolygon[(i + 1) % debugPolygon.Length];
            Gizmos.DrawLine(a, b);
        }

        // Bounds của khu vực
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(transform.position, areaBoundsSize);
    }
#endif
}
