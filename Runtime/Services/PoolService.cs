using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RecyclerKit
{
    /// <summary>
    /// Main pool service implementation.
    /// Pure C# logic with minimal Unity coupling - MonoBehaviour wrapper handles lifecycle.
    /// </summary>
    public class PoolService : IPoolService
    {
        private readonly Dictionary<int, IPoolBin> _instanceIdToBin = new();
        private readonly Dictionary<string, int> _nameToPrefabId = new();
        private readonly List<IPoolBin> _allBins = new();
        
        private readonly Transform _poolParent;
        private readonly Func<IEnumerator, Coroutine> _startCoroutine;
        private readonly Func<GameObject, Transform, GameObject> _instantiateFunc;
        
        /// <summary>
        /// Creates a new pool service.
        /// </summary>
        /// <param name="poolParent">Parent transform for pooled objects.</param>
        /// <param name="startCoroutine">Coroutine runner for delayed despawn.</param>
        /// <param name="instantiateFunc">Optional factory for creating instances.</param>
        public PoolService(
            Transform poolParent,
            Func<IEnumerator, Coroutine> startCoroutine,
            Func<GameObject, Transform, GameObject> instantiateFunc = null)
        {
            _poolParent = poolParent;
            _startCoroutine = startCoroutine;
            _instantiateFunc = instantiateFunc;
        }
        
        public void RegisterPool(IPoolConfig config)
        {
            if (config?.Prefab == null)
            {
                Debug.LogError("PoolService: Cannot register pool with null prefab");
                return;
            }
            
            string poolName = config.Prefab.name;
            
            if (_nameToPrefabId.ContainsKey(poolName))
            {
                Debug.LogError($"PoolService: Pool '{poolName}' is already registered");
                return;
            }
            
            // Create bin based on config - use async for Unity 6 hitless pool growth
            IPoolBin bin;
            if (config.UseAsyncInstantiation)
            {
                var asyncBin = new AsyncPoolBin(config, _poolParent, _startCoroutine);
                asyncBin.Initialize();
                bin = asyncBin;
            }
            else
            {
                var syncBin = new PoolBin(config, _poolParent, _instantiateFunc);
                syncBin.Initialize();
                bin = syncBin;
            }
            
            int instanceId = config.Prefab.GetInstanceID();
            _instanceIdToBin[instanceId] = bin;
            _nameToPrefabId[poolName] = instanceId;
            _allBins.Add(bin);
            
            // Setup auto-recycle for particles
            if (config.AutoRecycleParticles)
            {
                bin.OnSpawned += OnParticleSpawned;
            }
        }
        
        public void UnregisterPool(string poolName, bool destroyObjects = true)
        {
            if (!_nameToPrefabId.TryGetValue(poolName, out int instanceId))
                return;
            
            if (_instanceIdToBin.TryGetValue(instanceId, out var bin))
            {
                bin.Clear(destroyObjects);
                _allBins.Remove(bin);
            }
            
            _instanceIdToBin.Remove(instanceId);
            _nameToPrefabId.Remove(poolName);
        }
        
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            
            int instanceId = prefab.GetInstanceID();
            
            if (!_instanceIdToBin.TryGetValue(instanceId, out var bin))
            {
                Debug.LogWarning($"PoolService: No pool for '{prefab.name}'. Instantiating directly.");
                return UnityEngine.Object.Instantiate(prefab, position, rotation);
            }
            
            return SpawnFromBin(bin, position, rotation);
        }
        
        public GameObject Spawn(string poolName, Vector3 position, Quaternion rotation)
        {
            if (!_nameToPrefabId.TryGetValue(poolName, out int instanceId))
            {
                Debug.LogError($"PoolService: No pool named '{poolName}'");
                return null;
            }
            
            if (!_instanceIdToBin.TryGetValue(instanceId, out var bin))
                return null;
            
            return SpawnFromBin(bin, position, rotation);
        }
        
        public void Despawn(GameObject go)
        {
            if (go == null) return;
            
            // Use PooledObject component for zero-allocation lookup
            var pooledObject = go.GetComponent<PooledObject>();
            if (pooledObject == null)
            {
                Debug.LogWarning($"PoolService: Cannot despawn '{go.name}' - missing PooledObject component");
                UnityEngine.Object.Destroy(go);
                return;
            }
            
            int instanceId = pooledObject.PrefabInstanceId;
            
            if (_instanceIdToBin.TryGetValue(instanceId, out var bin))
            {
                bin.Despawn(go);
            }
            else
            {
                UnityEngine.Object.Destroy(go);
            }
        }
        
        public void DespawnAfterDelay(GameObject go, float delayInSeconds)
        {
            if (go == null) return;
            _startCoroutine(DespawnDelayedCoroutine(go, delayInSeconds));
        }
        
        public IPoolBin GetPoolBin(GameObject prefab)
        {
            if (prefab == null) return null;
            _instanceIdToBin.TryGetValue(prefab.GetInstanceID(), out var bin);
            return bin;
        }
        
        public IPoolBin GetPoolBin(string poolName)
        {
            if (!_nameToPrefabId.TryGetValue(poolName, out int instanceId))
                return null;
            
            _instanceIdToBin.TryGetValue(instanceId, out var bin);
            return bin;
        }
        
        public void CullAllPools()
        {
            foreach (var bin in _allBins)
            {
                bin.CullExcess();
            }
        }
        
        public void HandleSceneChange()
        {
            // Remove non-persistent pools
            for (int i = _allBins.Count - 1; i >= 0; i--)
            {
                var bin = _allBins[i];
                if (!bin.Config.PersistBetweenScenes)
                {
                    UnregisterPool(bin.PoolName, true);
                }
            }
        }
        
        private GameObject SpawnFromBin(IPoolBin bin, Vector3 position, Quaternion rotation)
        {
            var go = bin.Spawn();
            if (go == null) return null;
            
            var t = go.transform;
            
            // Detach from pool parent
            if (t is RectTransform)
                t.SetParent(null, false);
            else
                t.parent = null;
            
            t.position = position;
            t.rotation = rotation;
            
            // Only call SetActive if using SetActive hiding method
            // Layer/Position hiding keeps object active for GPU Resident Drawer
            if (bin.Config.Hiding == HidingMethod.SetActive)
            {
                go.SetActive(true);
            }
            
            return go;
        }
        
        private void OnParticleSpawned(GameObject go)
        {
            var ps = go.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // Despawn after particle system completes
                // Note: main.startLifetime is now a MinMaxCurve, use main.duration + a buffer
                var main = ps.main;
                float duration = main.duration + main.startLifetime.constantMax;
                DespawnAfterDelay(go, duration);
            }
            else
            {
                Debug.LogError($"PoolService: AutoRecycleParticles enabled but '{go.name}' has no ParticleSystem!");
            }
        }
        
        private IEnumerator DespawnDelayedCoroutine(GameObject go, float delay)
        {
            yield return new WaitForSeconds(delay);
            Despawn(go);
        }
    }
}
