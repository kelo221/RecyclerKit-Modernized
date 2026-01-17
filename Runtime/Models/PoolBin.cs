using System;
using UnityEngine;
using UnityEngine.Pool;

namespace RecyclerKit
{
    /// <summary>
    /// Pool bin using Unity's ObjectPool for optimal cache locality.
    /// Requires PooledObject component on prefabs for zero-allocation despawn.
    /// </summary>
    public sealed class PoolBin : IPoolBin
    {
        public event Action<GameObject> OnSpawned;
        public event Action<GameObject> OnDespawned;
        
        private readonly ObjectPool<GameObject> _pool;
        private readonly IPoolConfig _config;
        private readonly Transform _poolParent;
        private readonly int _prefabInstanceId;
        private readonly string _poolName;
        
        private static readonly Vector3 HiddenPosition = new Vector3(0, -10000, 0);
        
        private int _spawnedCount;
        private float _timeOfLastCull = float.MinValue;
        
        public string PoolName => _poolName;
        public int PrefabInstanceId => _prefabInstanceId;
        public int AvailableCount => _pool.CountInactive;
        public int SpawnedCount => _spawnedCount;
        public IPoolConfig Config => _config;
        
        public PoolBin(IPoolConfig config, Transform poolParent, Func<GameObject, Transform, GameObject> instantiateFunc = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _poolParent = poolParent;
            _prefabInstanceId = config.Prefab.GetInstanceID();
            _poolName = config.Prefab.name;
            
            _pool = new ObjectPool<GameObject>(
                createFunc: CreateInstance,
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
            var preallocated = new GameObject[_config.PreallocateCount];
            for (int i = 0; i < _config.PreallocateCount; i++)
            {
                preallocated[i] = _pool.Get();
            }
            for (int i = 0; i < _config.PreallocateCount; i++)
            {
                _pool.Release(preallocated[i]);
            }
            _spawnedCount = 0;
        }
        
        public GameObject Spawn()
        {
            if (_config.UseHardLimit && _spawnedCount >= _config.HardLimit)
                return null;
            
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
        
        #region ObjectPool Callbacks
        
        private GameObject CreateInstance()
        {
            var go = UnityEngine.Object.Instantiate(_config.Prefab, _poolParent);
            go.name = _poolName; // Remove "(Clone)"
            
            // Initialize PooledObject component
            var pooledObject = go.GetComponent<PooledObject>();
            if (pooledObject == null)
            {
                Debug.LogError($"PoolBin: Prefab '{_poolName}' missing required PooledObject component!");
                pooledObject = go.AddComponent<PooledObject>();
            }
            pooledObject.Initialize(_prefabInstanceId, _poolName);
            
            // Initially hide
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
                    // Activated by PoolService after positioning
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
