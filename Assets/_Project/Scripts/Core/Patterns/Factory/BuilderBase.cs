using UnityEngine;

namespace GameTemplate.Core.Patterns.Factory
{
    /// <summary>
    /// Builder pattern - tạo object có nhiều tham số tùy chọn mà không cần overload constructor 10 cái.
    /// Fluent API: chain method, đọc code như đọc câu.
    ///
    /// Cách dùng (vd RPG enemy spawn với tùy chọn):
    ///   var enemy = new EnemyBuilder()
    ///       .WithId("slime")
    ///       .AtPosition(spawnPoint)
    ///       .WithLevel(5)
    ///       .WithModifier(EnemyModifier.Elite)
    ///       .WithLoot(lootTable)
    ///       .Build();
    ///
    /// Có thể tạo Builder cho bất cứ class nào, đây là mẫu generic.
    /// </summary>
    public abstract class BuilderBase<TSelf, TProduct>
        where TSelf : BuilderBase<TSelf, TProduct>
        where TProduct : class
    {
        protected Vector3 _position = Vector3.zero;
        protected Quaternion _rotation = Quaternion.identity;
        protected Transform _parent;

        public TSelf AtPosition(Vector3 pos)
        {
            _position = pos;
            return (TSelf)this;
        }

        public TSelf AtPosition(Transform transform)
        {
            _position = transform.position;
            _rotation = transform.rotation;
            return (TSelf)this;
        }

        public TSelf WithRotation(Quaternion rot)
        {
            _rotation = rot;
            return (TSelf)this;
        }

        public TSelf WithParent(Transform parent)
        {
            _parent = parent;
            return (TSelf)this;
        }

        /// <summary>Override để return product cuối cùng.</summary>
        public abstract TProduct Build();
    }
}
