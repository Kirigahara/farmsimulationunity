namespace GameTemplate.Core.Pooling
{
    /// <summary>
    /// Interface cho object cần reset state khi despawn/respawn từ pool.
    ///
    /// Vì sao cần?
    ///   Pool reuse object cũ - state cũ (HP, position, velocity, color...) còn dính lại.
    ///   Vd: bullet bay xong despawn → spawn lại thấy bullet ở vị trí cũ trong 1 frame.
    ///
    /// Implement trên MonoBehaviour của object:
    ///   - OnSpawnFromPool: reset state về initial (HP full, velocity zero, alpha 1...)
    ///   - OnDespawnToPool: cleanup (stop coroutine, unsubscribe event...)
    ///
    /// PoolManager hoặc ObjectPool tự gọi 2 callback này khi Get/Release.
    /// Nhiều IPoolable trên cùng GameObject đều được gọi.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>Gọi NGAY trước khi object được spawn (sau SetActive(true)).</summary>
        void OnSpawnFromPool();

        /// <summary>Gọi NGAY trước khi object về pool (trước SetActive(false)).</summary>
        void OnDespawnToPool();
    }
}
