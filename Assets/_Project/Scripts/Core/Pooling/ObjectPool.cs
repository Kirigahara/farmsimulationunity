using System.Collections.Generic;
using UnityEngine;

namespace GameTemplate.Core.Pooling
{
    /// <summary>
    /// Generic Object Pool cho bullet, enemy, VFX, particle...
    /// Mobile: GC spike = frame drop. Phải pool mọi thứ spawn/despawn liên tục.
    ///
    /// Cách dùng cơ bản:
    ///   var pool = new ObjectPool&lt;Bullet&gt;(bulletPrefab, prewarm: 50);
    ///   var bullet = pool.Get();
    ///   pool.Release(bullet);
    ///
    /// Cách dùng đầy đủ:
    ///   var pool = new ObjectPool&lt;Bullet&gt;(
    ///       bulletPrefab,
    ///       prewarm: 50,
    ///       maxSize: 200,           // cap chống memory leak
    ///       parent: container,
    ///       expandable: true);
    ///
    /// Features:
    ///   - MaxSize cap: pool không phình quá max (return null nếu cạn)
    ///   - IPoolable callback: object tự reset state khi spawn/despawn
    ///   - Active tracking: CountActive, CountTotal cho debug
    ///   - DespawnAll(): bulk return tất cả active (vd: scene reset)
    ///   - Backward compatible: code cũ dùng `new ObjectPool&lt;T&gt;(prefab, size)` vẫn work
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _available;
        private readonly HashSet<T> _active;
        private readonly bool _expandable;
        private readonly int _maxSize;

        // ===== Public stats (cho debug + Editor inspector) =====

        /// <summary>Số instance đang sẵn sàng để Get() trong pool (inactive).</summary>
        public int CountAvailable => _available.Count;

        /// <summary>Số instance đang được dùng (đã Get nhưng chưa Release).</summary>
        public int CountActive => _active.Count;

        /// <summary>Tổng instance (active + available). Pool sẽ KHÔNG vượt MaxSize.</summary>
        public int CountTotal => CountAvailable + CountActive;

        /// <summary>Max instance pool có thể có. 0 hoặc âm = không giới hạn.</summary>
        public int MaxSize => _maxSize;

        /// <summary>Prefab gốc - dùng để debug/Inspector.</summary>
        public T Prefab => _prefab;

        // ============================================================
        // CONSTRUCTORS
        // ============================================================

        /// <summary>
        /// Constructor đầy đủ - dùng cho production code.
        /// </summary>
        /// <param name="prefab">Prefab gốc, mỗi instance là Clone.</param>
        /// <param name="prewarm">Số instance tạo sẵn (tránh GC spike lần đầu Get).</param>
        /// <param name="maxSize">Số instance tối đa (active + available). 0 = unlimited.</param>
        /// <param name="parent">Transform parent cho instance.</param>
        /// <param name="expandable">True = pool cạn vẫn tạo mới (đến maxSize). False = return null.</param>
        public ObjectPool(T prefab, int prewarm, int maxSize, Transform parent = null, bool expandable = true)
        {
            if (prefab == null)
            {
                Debug.LogError("[ObjectPool] Prefab null - không thể tạo pool.");
                return;
            }

            _prefab = prefab;
            _parent = parent;
            _expandable = expandable;
            _maxSize = maxSize <= 0 ? int.MaxValue : maxSize;
            _available = new Stack<T>(prewarm);
            _active = new HashSet<T>();

            // Clamp prewarm không vượt maxSize
            int actualPrewarm = Mathf.Min(prewarm, _maxSize);
            for (int i = 0; i < actualPrewarm; i++)
            {
                var instance = CreateNew();
                instance.gameObject.SetActive(false);
                _available.Push(instance);
            }
        }

        /// <summary>
        /// Constructor BACKWARD COMPATIBLE - khớp signature cũ trước khi add MaxSize.
        /// Mặc định MaxSize = unlimited để giữ behavior cũ.
        /// </summary>
        public ObjectPool(T prefab, int initialSize, Transform parent = null, bool expandable = true)
            : this(prefab, initialSize, 0, parent, expandable)
        {
        }

        // ============================================================
        // GET (SPAWN)
        // ============================================================

        /// <summary>
        /// Lấy 1 instance từ pool. Return null nếu pool cạn và không expandable hoặc đã đạt MaxSize.
        /// </summary>
        public T Get()
        {
            T instance;

            // Lấy từ pool có sẵn
            if (_available.Count > 0)
            {
                instance = _available.Pop();
            }
            // Pool cạn - tạo mới nếu allow expand VÀ chưa đạt MaxSize
            else if (_expandable && CountTotal < _maxSize)
            {
                instance = CreateNew();
            }
            else
            {
                if (CountTotal >= _maxSize)
                {
                    Debug.LogWarning(
                        $"[ObjectPool<{typeof(T).Name}>] Đã đạt MaxSize ({_maxSize}). " +
                        "Tăng MaxSize hoặc despawn instance cũ trước.");
                }
                return null;
            }

            // Active GameObject TRƯỚC khi gọi callback (callback có thể truy cập component)
            instance.gameObject.SetActive(true);
            _active.Add(instance);

            // Gọi IPoolable callback - object tự reset state
            // GetComponents (plural) - nhiều IPoolable trên cùng GameObject đều được gọi
            var poolables = instance.GetComponents<IPoolable>();
            for (int i = 0; i < poolables.Length; i++) poolables[i].OnSpawnFromPool();

            return instance;
        }

        // ============================================================
        // RELEASE (DESPAWN)
        // ============================================================

        /// <summary>
        /// Trả instance về pool. Tự gọi IPoolable.OnDespawnToPool trước khi SetActive(false).
        /// Silent skip nếu instance null hoặc không thuộc pool này.
        /// </summary>
        public void Release(T instance)
        {
            if (instance == null) return;

            // Verify instance thuộc pool này (chống developer release nhầm object)
            // Skip silent nếu không có - tránh log spam khi gọi 2 lần Release liên tiếp
            if (!_active.Contains(instance)) return;

            // Gọi callback TRƯỚC khi SetActive(false)
            var poolables = instance.GetComponents<IPoolable>();
            for (int i = 0; i < poolables.Length; i++) poolables[i].OnDespawnToPool();

            instance.gameObject.SetActive(false);

            // Reset parent về pool parent (nếu instance bị reparent ra ngoài trong lúc dùng)
            if (_parent != null && instance.transform.parent != _parent)
            {
                instance.transform.SetParent(_parent, worldPositionStays: false);
            }

            _active.Remove(instance);
            _available.Push(instance);
        }

        // ============================================================
        // BULK OPERATIONS
        // ============================================================

        /// <summary>
        /// Despawn TẤT CẢ instance đang active.
        /// Use case: player chết → clear hết bullet đang bay, scene transition reset.
        /// </summary>
        public void DespawnAll()
        {
            // Copy ra list vì sẽ modify _active trong loop
            var snapshot = new List<T>(_active);
            for (int i = 0; i < snapshot.Count; i++) Release(snapshot[i]);
        }

        /// <summary>
        /// Clear pool và destroy TẤT CẢ instance (cả active + available).
        /// Use case: scene unload, không pool nữa.
        /// </summary>
        public void Clear()
        {
            // Destroy active
            foreach (var inst in _active)
                if (inst != null) Object.Destroy(inst.gameObject);
            _active.Clear();

            // Destroy available
            while (_available.Count > 0)
            {
                var inst = _available.Pop();
                if (inst != null) Object.Destroy(inst.gameObject);
            }
        }

        // ============================================================
        // INTERNAL
        // ============================================================

        private T CreateNew()
        {
            var instance = Object.Instantiate(_prefab, _parent);
            // Đặt tên gọn không có "(Clone)" cho Inspector dễ nhìn
            instance.name = _prefab.name;
            return instance;
        }
    }
}
