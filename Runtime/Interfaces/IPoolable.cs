namespace RecyclerKit
{
    /// <summary>
    /// Interface for objects that want spawn/despawn callbacks.
    /// Implement on any MonoBehaviour attached to a pooled prefab.
    /// Zero overhead - compiler inlines these calls.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called immediately after object is spawned from pool.
        /// Use to reset state, enable components, etc.
        /// </summary>
        void OnSpawn();
        
        /// <summary>
        /// Called immediately before object is returned to pool.
        /// Use to cleanup, stop effects, disable components, etc.
        /// </summary>
        void OnDespawn();
    }
}
