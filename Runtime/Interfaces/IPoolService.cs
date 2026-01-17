using UnityEngine;

namespace RecyclerKit
{
    /// <summary>
    /// Main service interface for object pooling operations.
    /// Follows Dependency Inversion - high level modules depend on this abstraction.
    /// </summary>
    public interface IPoolService
    {
        /// <summary>
        /// Spawns an object from the pool for the given prefab.
        /// </summary>
        GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation);
        
        /// <summary>
        /// Spawns an object from the pool by name.
        /// </summary>
        GameObject Spawn(string poolName, Vector3 position, Quaternion rotation);
        
        /// <summary>
        /// Returns an object to its pool.
        /// </summary>
        void Despawn(GameObject go);
        
        /// <summary>
        /// Returns an object to its pool after a delay.
        /// </summary>
        void DespawnAfterDelay(GameObject go, float delayInSeconds);
        
        /// <summary>
        /// Gets the pool bin for a prefab.
        /// </summary>
        IPoolBin GetPoolBin(GameObject prefab);
        
        /// <summary>
        /// Gets the pool bin by name.
        /// </summary>
        IPoolBin GetPoolBin(string poolName);
        
        /// <summary>
        /// Registers a new pool at runtime.
        /// </summary>
        void RegisterPool(IPoolConfig config);
        
        /// <summary>
        /// Unregisters and optionally destroys a pool.
        /// </summary>
        void UnregisterPool(string poolName, bool destroyObjects = true);
        
        /// <summary>
        /// Culls excess objects from all registered pools.
        /// </summary>
        void CullAllPools();
        
        /// <summary>
        /// Clears pools that don't persist between scenes.
        /// </summary>
        void HandleSceneChange();
    }
}
