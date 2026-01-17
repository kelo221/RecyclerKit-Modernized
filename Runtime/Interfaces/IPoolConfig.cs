using UnityEngine;

namespace RecyclerKit
{
    /// <summary>
    /// Configuration for a single object pool.
    /// Follows Interface Segregation - only what pools need to know about their config.
    /// </summary>
    public interface IPoolConfig
    {
        /// <summary>
        /// The prefab to pool.
        /// </summary>
        GameObject Prefab { get; }
        
        /// <summary>
        /// Number of instances to create on initialization.
        /// </summary>
        int PreallocateCount { get; }
        
        /// <summary>
        /// Number of instances to create when pool is empty and more are needed.
        /// </summary>
        int GrowCount { get; }
        
        /// <summary>
        /// If true, pool will not grow beyond HardLimit.
        /// </summary>
        bool UseHardLimit { get; }
        
        /// <summary>
        /// Maximum instances when UseHardLimit is true.
        /// </summary>
        int HardLimit { get; }
        
        /// <summary>
        /// If true, ParticleSystem objects are auto-despawned after their duration.
        /// </summary>
        bool AutoRecycleParticles { get; }
        
        /// <summary>
        /// If true, pool survives scene changes.
        /// </summary>
        bool PersistBetweenScenes { get; }
        
        /// <summary>
        /// If true, use Unity 6 async instantiation for hitless pool growth.
        /// </summary>
        bool UseAsyncInstantiation { get; }
        
        /// <summary>
        /// Culling settings for this pool.
        /// </summary>
        ICullingConfig Culling { get; }
        
        /// <summary>
        /// Method used to hide pooled objects. Default: Layer (GPU Resident Drawer compatible).
        /// </summary>
        HidingMethod Hiding { get; }
        
        /// <summary>
        /// Layer for hidden objects when using HidingMethod.Layer. Default: 30.
        /// </summary>
        int HiddenLayer { get; }
    }
}
