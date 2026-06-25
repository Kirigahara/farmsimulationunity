using System.Collections.Generic;
using UnityEngine;
using GameTemplate.Core.Pooling;

namespace GameTemplate.Core.Patterns.Factory
{
    /// <summary>
    /// Generic Factory dùng ScriptableObject làm config + Object Pool bên trong.
    /// Designer thêm enemy mới chỉ cần tạo ScriptableObject - không cần đụng code.
    ///
    /// Cách dùng:
    ///   1. Tạo class data: class EnemyData : ScriptableObject với prefab + stats
    ///   2. Tạo class entity: class Enemy : MonoBehaviour, IConfigurable<EnemyData>
    ///   3. Tạo factory: var factory = new PrefabFactory<EnemyData, Enemy>(allEnemyData);
    ///   4. Spawn: var slime = factory.Create("slime_lv1", spawnPos);
    ///   5. Return: factory.Return(slime);
    /// </summary>
    public interface IConfigurable<TData> where TData : ScriptableObject
    {
        void Configure(TData data);
    }

    /// <summary>Base ScriptableObject - mọi data dùng cho Factory phải có Id để lookup.</summary>
    public abstract class FactoryDataBase : ScriptableObject
    {
        public string Id;
        public abstract GameObject Prefab { get; }
    }

    public class PrefabFactory<TData, TEntity>
        where TData : FactoryDataBase
        where TEntity : Component, IConfigurable<TData>
    {
        private readonly Dictionary<string, TData> _dataLookup = new Dictionary<string, TData>();
        private readonly Dictionary<string, ObjectPool<TEntity>> _pools = new Dictionary<string, ObjectPool<TEntity>>();
        private readonly Transform _poolParent;
        private readonly int _initialPoolSize;

        /// <summary>
        /// Khởi tạo factory với list data. Factory sẽ tự động tạo pool cho mỗi Id khi cần.
        /// </summary>
        /// <param name="allData">Danh sách tất cả dữ liệu để khởi tạo factory.</param>
        /// <param name="poolParent">Transform cha cho các object trong pool.</param>
        /// <param name="initialPoolSize">Kích thước ban đầu của pool, mặc định là 8.</param>
        public PrefabFactory(IEnumerable<TData> allData, Transform poolParent = null, int initialPoolSize = 8)
        {
            _poolParent = poolParent;
            _initialPoolSize = initialPoolSize;

            foreach (var data in allData)
            {
                if (string.IsNullOrEmpty(data.Id))
                {
                    Debug.LogWarning($"[Factory] Data {data.name} có Id rỗng, skip.");
                    continue;
                }
                _dataLookup[data.Id] = data;
            }
        }

        /// <summary>Tạo entity theo Id, set vị trí, gọi Configure để inject data vào.</summary>
        public TEntity Create(string id, Vector3 position = default, Quaternion rotation = default)
        {
            if (!_dataLookup.TryGetValue(id, out var data))
            {
                Debug.LogError($"[Factory] Không tìm thấy Id '{id}'.");
                return null;
            }

            // Lazy init pool theo Id
            if (!_pools.TryGetValue(id, out var pool))
            {
                var prefabComponent = data.Prefab.GetComponent<TEntity>();
                if (prefabComponent == null)
                {
                    Debug.LogError($"[Factory] Prefab '{data.Prefab.name}' không có {typeof(TEntity).Name}.");
                    return null;
                }
                pool = new ObjectPool<TEntity>(prefabComponent, _initialPoolSize, _poolParent);
                _pools[id] = pool;
            }

            var entity = pool.Get();
            entity.transform.SetPositionAndRotation(position, rotation == default ? Quaternion.identity : rotation);
            entity.Configure(data);
            return entity;
        }

        /// <summary>Trả entity về pool. Cần biết Id để return đúng pool.</summary>
        public void Return(string id, TEntity entity)
        {
            if (_pools.TryGetValue(id, out var pool))
                pool.Release(entity);
            else
                Object.Destroy(entity.gameObject);
        }
    }
}
