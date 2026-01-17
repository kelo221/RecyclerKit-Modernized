using System;
using UnityEngine;
using UnityEngine.Events;

namespace RecyclerKit
{
    /// <summary>
    /// Required component on all pooled objects.
    /// Provides zero-allocation despawn and per-object spawn/despawn events.
    /// </summary>
    [DisallowMultipleComponent]
    public class PooledObject : MonoBehaviour
    {
        #region Cached Data
        
        // Cached to avoid GameObject.name allocation on despawn
        private int _prefabInstanceId;
        private string _poolName;
        
        /// <summary>
        /// Prefab instance ID this object belongs to.
        /// </summary>
        public int PrefabInstanceId => _prefabInstanceId;
        
        /// <summary>
        /// Pool name (cached from prefab.name).
        /// </summary>
        public string PoolName => _poolName;
        
        #endregion
        
        #region Events
        
        [Header("Events (Optional)")]
        [Tooltip("Called when spawned from pool")]
        public UnityEvent onSpawn;
        
        [Tooltip("Called before returning to pool")]
        public UnityEvent onDespawn;
        
        #endregion
        
        #region Component Disabling
        
        [Header("Components to Disable When Pooled")]
        [Tooltip("MonoBehaviours to disable (stops Update/FixedUpdate)")]
        [SerializeField]
        private MonoBehaviour[] behavioursToDisable;
        
        [Tooltip("Rigidbodies to make kinematic")]
        [SerializeField]
        private Rigidbody[] rigidbodiesToDisable;
        
        [Tooltip("Colliders to disable")]
        [SerializeField]
        private Collider[] collidersToDisable;
        
        private bool[] _originalKinematicStates;
        
        #endregion
        
        #region IPoolable Cache
        
        // Cached IPoolable implementations on this object
        private IPoolable[] _poolables;
        private bool _poolablesCached;
        
        #endregion
        
        /// <summary>
        /// Initializes the component with pool data. Called by PoolBin.
        /// </summary>
        public void Initialize(int prefabInstanceId, string poolName)
        {
            _prefabInstanceId = prefabInstanceId;
            _poolName = poolName;
            
            // Cache original kinematic states
            if (rigidbodiesToDisable != null && rigidbodiesToDisable.Length > 0)
            {
                _originalKinematicStates = new bool[rigidbodiesToDisable.Length];
                for (int i = 0; i < rigidbodiesToDisable.Length; i++)
                {
                    if (rigidbodiesToDisable[i] != null)
                        _originalKinematicStates[i] = rigidbodiesToDisable[i].isKinematic;
                }
            }
            
            // Cache IPoolable implementations
            CachePoolables();
        }
        
        /// <summary>
        /// Called when object is spawned from pool.
        /// </summary>
        public void NotifySpawned()
        {
            // Re-enable components
            EnableComponents();
            
            // Notify IPoolable implementations
            if (_poolables != null)
            {
                foreach (var poolable in _poolables)
                    poolable.OnSpawn();
            }
            
            // Fire UnityEvent
            onSpawn?.Invoke();
        }
        
        /// <summary>
        /// Called when object is returned to pool.
        /// </summary>
        public void NotifyDespawned()
        {
            // Fire UnityEvent first (so listeners can cleanup)
            onDespawn?.Invoke();
            
            // Notify IPoolable implementations
            if (_poolables != null)
            {
                foreach (var poolable in _poolables)
                    poolable.OnDespawn();
            }
            
            // Disable components
            DisableComponents();
        }
        
        private void CachePoolables()
        {
            if (_poolablesCached) return;
            _poolables = GetComponentsInChildren<IPoolable>(true) as IPoolable[];
            
            // GetComponentsInChildren returns Component[], need to filter
            var components = GetComponentsInChildren<MonoBehaviour>(true);
            var poolableList = new System.Collections.Generic.List<IPoolable>();
            foreach (var comp in components)
            {
                if (comp is IPoolable poolable && !(comp is PooledObject))
                    poolableList.Add(poolable);
            }
            _poolables = poolableList.Count > 0 ? poolableList.ToArray() : null;
            _poolablesCached = true;
        }
        
        private void EnableComponents()
        {
            if (behavioursToDisable != null)
            {
                foreach (var b in behavioursToDisable)
                    if (b != null) b.enabled = true;
            }
            
            if (rigidbodiesToDisable != null && _originalKinematicStates != null)
            {
                for (int i = 0; i < rigidbodiesToDisable.Length; i++)
                    if (rigidbodiesToDisable[i] != null)
                        rigidbodiesToDisable[i].isKinematic = _originalKinematicStates[i];
            }
            
            if (collidersToDisable != null)
            {
                foreach (var c in collidersToDisable)
                    if (c != null) c.enabled = true;
            }
        }
        
        private void DisableComponents()
        {
            if (behavioursToDisable != null)
            {
                foreach (var b in behavioursToDisable)
                    if (b != null) b.enabled = false;
            }
            
            if (rigidbodiesToDisable != null)
            {
                foreach (var rb in rigidbodiesToDisable)
                    if (rb != null) rb.isKinematic = true;
            }
            
            if (collidersToDisable != null)
            {
                foreach (var c in collidersToDisable)
                    if (c != null) c.enabled = false;
            }
        }
    }
}
