using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace RecyclerKit
{
    /// <summary>
    /// Pool bin with async preallocation support.
    /// Requires PooledObject component on prefabs for zero-allocation despawn.
    /// </summary>
    public sealed class AsyncPoolBin : IPoolBin
    {
        public event Action<GameObject> OnSpawned;
        public event Action<GameObject> OnDespawned;
        
        private readonly ObjectPool<GameObject> _pool;
        private readonly IPoolConfig _config;
        private readonly Transform _poolParent;
        private readonly Func<IEnumerator, Coroutine> _startCoroutine;
        private readonly int _prefabInstanceId;
        private readonly string _poolName;
        
        private static readonly Vector3 HiddenPosition = new Vector3(0, -10000, 0);
        
        private int _spawnedCount;
        private int _pendingInstantiations;
        private float _timeOfLastCull = float.MinValue;
        
        public string PoolName => _poolName;
        public int PrefabInstanceId => _prefabInstanceId;
        public int AvailableCount => _pool.CountInactive;
        public int SpawnedCount => _spawnedCount;
        public IPoolConfig Config => _config;
        public int PendingInstantiations => _pendingInstantiations;
        
        public AsyncPoolBin(IPoolConfig config, Transform poolParent, Func<IEnumerator, Coroutine> startCoroutine)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _poolParent = poolParent;
            _startCoroutine = startCoroutine ?? throw new ArgumentNullException(nameof(startCoroutine));
            _prefabInstanceId = config.Prefab.GetInstanceID();
            _poolName = config.Prefab.name;
            
            _pool = new ObjectPool<GameObject>(
                createFunc: CreateInstanceSync,
                actionOnGet: OnGetFromPool,
                actionOnRelease: OnReturnToPool,
                actionOnDestroy: OnDestroyPooled,
                collectionCheck: false,
                defaultCapacity: config.PreallocateCount,
                maxSize: config.UseHardLimit ? config.HardLimit : 10000
            );
        }
        
        public void Initialize()
        {
            if (_config.PreallocateCount > 0)
            {
                PreallocateAsync(_config.PreallocateCount);
            }
        }
        
        public GameObject Spawn()
        {
            if (_config.UseHardLimit && _spawnedCount >= _config.HardLimit)
                return null;
            
            if (_pool.CountInactive <= 1 && _pendingInstantiations == 0)
            {
                int growAmount = _config.GrowCount;
                if (_config.UseHardLimit)
                {
                    int remaining = _config.HardLimit - _spawnedCount - _pool.CountInactive - _pendingInstantiations;
                    growAmount = Mathf.Min(growAmount, remaining);
                }
                if (growAmount > 0)
                {
                    PreallocateAsync(growAmount);
                }
            }
            
            var go = _pool.Get();
            _spawnedCount++;
            
            OnSpawned?.Invoke(go);
            return go;
        }
        
        public void Despawn(GameObject go)
        {
            if (go == null) return;
            
            _pool.Release(go);
            _spawnedCount--;
            
            OnDespawned?.Invoke(go);
        }
        
        public void Clear(bool destroyObjects)
        {
            _pool.Clear();
            _spawnedCount = 0;
        }
        
        public void CullExcess()
        {
            if (!_config.Culling.Enabled) return;
            if (_pool.CountInactive <= _config.Culling.MaintainCount) return;
            if (Time.time < _timeOfLastCull + _config.Culling.Interval) return;
            
            _timeOfLastCull = Time.time;
            
            int excess = _pool.CountInactive - _config.Culling.MaintainCount;
            for (int i = 0; i < excess; i++)
            {
                var go = _pool.Get();
                UnityEngine.Object.Destroy(go);
            }
            _spawnedCount -= excess;
        }
        
        private void PreallocateAsync(int count)
        {
            _pendingInstantiations += count;
            _startCoroutine(PreallocateCoroutine(count));
        }
        
        private IEnumerator PreallocateCoroutine(int count)
        {
            var asyncOp = UnityEngine.Object.InstantiateAsync(_config.Prefab, count, _poolParent);
            yield return asyncOp;
            
            foreach (var go in asyncOp.Result)
            {
                go.name = _poolName;
                
                // Initialize PooledObject
                var pooledObject = go.GetComponent<PooledObject>();
                if (pooledObject == null)
                {
                    Debug.LogError($"AsyncPoolBin: Prefab '{_poolName}' missing required PooledObject component!");
                    pooledObject = go.AddComponent<PooledObject>();
                }
                pooledObject.Initialize(_prefabInstanceId, _poolName);
                
                HideObject(go, pooledObject);
                _pool.Release(go);
                _pendingInstantiations--;
            }
        }
        
        #region ObjectPool Callbacks
        
        private GameObject CreateInstanceSync()
        {
            var go = UnityEngine.Object.Instantiate(_config.Prefab, _poolParent);
            go.name = _poolName;
            
            var pooledObject = go.GetComponent<PooledObject>();
            if (pooledObject == null)
            {
                Debug.LogError($"AsyncPoolBin: Prefab '{_poolName}' missing required PooledObject component!");
                pooledObject = go.AddComponent<PooledObject>();
            }
            pooledObject.Initialize(_prefabInstanceId, _poolName);
            
            HideObject(go, pooledObject);
            return go;
        }
        
        private void OnGetFromPool(GameObject go)
        {
            var pooledObject = go.GetComponent<PooledObject>();
            ShowObject(go, pooledObject);
            pooledObject?.NotifySpawned();
        }
        
        private void OnReturnToPool(GameObject go)
        {
            var pooledObject = go.GetComponent<PooledObject>();
            pooledObject?.NotifyDespawned();
            HideObject(go, pooledObject);
            SetParent(go.transform, _poolParent);
        }
        
        private void OnDestroyPooled(GameObject go)
        {
            if (go != null)
                UnityEngine.Object.Destroy(go);
        }
        
        #endregion
        
        #region Hiding Methods
        
        private void HideObject(GameObject go, PooledObject pooledObject)
        {
            switch (_config.Hiding)
            {
                case HidingMethod.SetActive:
                    go.SetActive(false);
                    break;
                    
                case HidingMethod.Layer:
                    SetLayerRecursively(go, _config.HiddenLayer);
                    break;
                    
                case HidingMethod.Position:
                    go.transform.position = HiddenPosition;
                    break;
            }
        }
        
        private void ShowObject(GameObject go, PooledObject pooledObject)
        {
            switch (_config.Hiding)
            {
                case HidingMethod.SetActive:
                    // Activated by PoolService
                    break;
                    
                case HidingMethod.Layer:
                    SetLayerRecursively(go, _config.Prefab.layer);
                    break;
                    
                case HidingMethod.Position:
                    // Position set by PoolService
                    break;
            }
        }
        
        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            var t = go.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                SetLayerRecursively(t.GetChild(i).gameObject, layer);
            }
        }
        
        private static void SetParent(Transform child, Transform parent)
        {
            if (child is RectTransform)
                child.SetParent(parent, false);
            else
                child.parent = parent;
        }
        
        #endregion
    }
}
