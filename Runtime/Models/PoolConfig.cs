using System;
using UnityEngine;

namespace RecyclerKit
{
    /// <summary>
    /// Configuration for a single object pool.
    /// Serializable for Unity Inspector usage.
    /// </summary>
    [Serializable]
    public class PoolConfig : IPoolConfig
    {
        [SerializeField]
        [Tooltip("The prefab to pool")]
        private GameObject prefab;
        
        [SerializeField]
        [Tooltip("Number of instances to create at initialization")]
        private int preallocateCount = 5;
        
        [SerializeField]
        [Tooltip("Number of instances to create when pool is empty")]
        private int growCount = 1;
        
        [SerializeField]
        [Tooltip("If true, pool will not grow beyond hard limit")]
        private bool useHardLimit;
        
        [SerializeField]
        [Tooltip("Maximum instances when hard limit is enabled")]
        private int hardLimit = 50;
        
        [SerializeField]
        [Tooltip("If true, ParticleSystem objects are auto-despawned after their duration")]
        private bool autoRecycleParticles;
        
        [SerializeField]
        [Tooltip("If true, pool survives scene changes")]
        private bool persistBetweenScenes;
        
        [SerializeField]
        [Tooltip("Use Unity 6 async instantiation for hitless pool growth")]
        private bool useAsyncInstantiation = true;
        
        [SerializeField]
        private CullingConfig culling = new CullingConfig();
        
        [Header("Hiding (GPU Resident Drawer)")]
        [SerializeField]
        [Tooltip("How to hide pooled objects. Layer is GPU Resident Drawer compatible.")]
        private HidingMethod hiding = HidingMethod.Layer;
        
        [SerializeField]
        [Tooltip("Layer for hidden objects when using Layer hiding method")]
        private int hiddenLayer = 30;
        
        // IPoolConfig implementation
        public GameObject Prefab => prefab;
        public int PreallocateCount => preallocateCount;
        public int GrowCount => growCount;
        public bool UseHardLimit => useHardLimit;
        public int HardLimit => hardLimit;
        public bool AutoRecycleParticles => autoRecycleParticles;
        public bool PersistBetweenScenes => persistBetweenScenes;
        public bool UseAsyncInstantiation => useAsyncInstantiation;
        public ICullingConfig Culling => culling;
        public HidingMethod Hiding => hiding;
        public int HiddenLayer => hiddenLayer;
        
        public PoolConfig() { }
        
        public PoolConfig(GameObject prefab)
        {
            this.prefab = prefab;
        }
        
        /// <summary>
        /// Builder pattern for fluent configuration.
        /// </summary>
        public PoolConfig WithPreallocate(int count)
        {
            preallocateCount = Mathf.Max(0, count);
            return this;
        }
        
        public PoolConfig WithGrowCount(int count)
        {
            growCount = Mathf.Max(1, count);
            return this;
        }
        
        public PoolConfig WithHardLimit(int limit)
        {
            useHardLimit = true;
            hardLimit = Mathf.Max(1, limit);
            return this;
        }
        
        public PoolConfig WithAutoRecycleParticles()
        {
            autoRecycleParticles = true;
            return this;
        }
        
        public PoolConfig WithPersistence()
        {
            persistBetweenScenes = true;
            return this;
        }
        
        public PoolConfig WithCulling(int maintainCount, float interval)
        {
            culling = new CullingConfig(true, maintainCount, interval);
            return this;
        }
        
        /// <summary>
        /// Use synchronous instantiation (legacy behavior).
        /// </summary>
        public PoolConfig WithSyncInstantiation()
        {
            useAsyncInstantiation = false;
            return this;
        }
        
        /// <summary>
        /// Use Unity 6 async instantiation for hitless pool growth.
        /// </summary>
        public PoolConfig WithAsyncInstantiation()
        {
            useAsyncInstantiation = true;
            return this;
        }
        
        /// <summary>
        /// Use layer-based hiding (GPU Resident Drawer compatible).
        /// </summary>
        public PoolConfig WithLayerHiding(int layer = 30)
        {
            hiding = HidingMethod.Layer;
            hiddenLayer = layer;
            return this;
        }
        
        /// <summary>
        /// Use SetActive for hiding (legacy, less performant).
        /// </summary>
        public PoolConfig WithSetActiveHiding()
        {
            hiding = HidingMethod.SetActive;
            return this;
        }
        
        /// <summary>
        /// Use position-based hiding (move far off-screen).
        /// </summary>
        public PoolConfig WithPositionHiding()
        {
            hiding = HidingMethod.Position;
            return this;
        }
    }
}
