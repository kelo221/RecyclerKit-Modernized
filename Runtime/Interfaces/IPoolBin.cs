using System;
using UnityEngine;

namespace RecyclerKit
{
    /// <summary>
    /// Interface for a single pool bin that manages instances of one prefab.
    /// Follows Single Responsibility - only handles its own pool of objects.
    /// </summary>
    public interface IPoolBin
    {
        /// <summary>
        /// Fired when a GameObject is spawned from this bin.
        /// </summary>
        event Action<GameObject> OnSpawned;
        
        /// <summary>
        /// Fired when a GameObject is returned to this bin.
        /// </summary>
        event Action<GameObject> OnDespawned;
        
        /// <summary>
        /// Name of this pool (matches prefab name).
        /// </summary>
        string PoolName { get; }
        
        /// <summary>
        /// Instance ID of the prefab this bin manages.
        /// </summary>
        int PrefabInstanceId { get; }
        
        /// <summary>
        /// Number of objects currently available in the pool.
        /// </summary>
        int AvailableCount { get; }
        
        /// <summary>
        /// Number of objects currently spawned (in use).
        /// </summary>
        int SpawnedCount { get; }
        
        /// <summary>
        /// Configuration for this pool.
        /// </summary>
        IPoolConfig Config { get; }
        
        /// <summary>
        /// Gets an object from the pool. May return null if hard limit reached.
        /// </summary>
        GameObject Spawn();
        
        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        void Despawn(GameObject go);
        
        /// <summary>
        /// Clears all pooled objects.
        /// </summary>
        /// <param name="destroyObjects">If true, destroys the GameObjects.</param>
        void Clear(bool destroyObjects);
        
        /// <summary>
        /// Culls excess objects beyond the maintain count.
        /// </summary>
        void CullExcess();
    }
}
