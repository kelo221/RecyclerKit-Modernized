using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RecyclerKit
{
    /// <summary>
    /// Main entry point for the object pooling system.
    /// Thin MonoBehaviour facade that delegates to IPoolService.
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        #region Singleton
        
        private static PoolManager _instance;
        
        public static PoolManager Instance
        {
            get
            {
                if (_instance == null)
                    Debug.LogError("PoolManager instance is null. Ensure a PoolManager exists in the scene.");
                return _instance;
            }
        }
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Pool Settings")]
        [SerializeField]
        [Tooltip("List of pools to pre-configure")]
        private List<PoolConfig> pools = new();
        
        [Header("Culling")]
        [SerializeField]
        [Tooltip("How often to cull excess objects (0 = disabled)")]
        private float cullInterval = 10f;
        
        [Header("Persistence")]
        [SerializeField]
        [Tooltip("If true, this PoolManager survives scene changes")]
        private bool persistBetweenScenes;
        
        #endregion
        
        #region Runtime State
        
        private IPoolService _poolService;
        private NativePoolLookup _nativeLookup;
        private Coroutine _cullCoroutine;
        
        // Component cache for generic Spawn<T>
        private readonly Dictionary<int, Dictionary<Type, Component>> _componentCache = new();
        
        public IPoolService Service => _poolService;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            if (persistBetweenScenes)
                DontDestroyOnLoad(gameObject);
            
            InitializeService();
            
            SceneManager.activeSceneChanged += OnSceneChanged;
        }
        
        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            
            // CRITICAL: Dispose native collections to prevent memory leaks
            if (_nativeLookup.IsCreated)
                _nativeLookup.Dispose();
            
            if (_instance == this)
                _instance = null;
        }
        
        private void OnApplicationQuit()
        {
            // Dispose before quit to avoid Unity 6 native memory warnings
            if (_nativeLookup.IsCreated)
                _nativeLookup.Dispose();
            
            _instance = null;
        }
        
        #endregion
        
        #region Static API
        
        /// <summary>
        /// Spawns an object from the pool.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position = default, Quaternion rotation = default)
        {
            return Instance?._poolService?.Spawn(prefab, position, rotation);
        }
        
        /// <summary>
        /// Spawns an object and returns a cached component. Avoids GetComponent call.
        /// </summary>
        public static T Spawn<T>(GameObject prefab, Vector3 position = default, Quaternion rotation = default) where T : Component
        {
            var go = Instance?._poolService?.Spawn(prefab, position, rotation);
            if (go == null) return null;
            
            return Instance.GetCachedComponent<T>(go);
        }
        
        /// <summary>
        /// Spawns an object from the pool by name.
        /// </summary>
        public static GameObject Spawn(string poolName, Vector3 position = default, Quaternion rotation = default)
        {
            return Instance?._poolService?.Spawn(poolName, position, rotation);
        }
        
        /// <summary>
        /// Spawns by name and returns cached component.
        /// </summary>
        public static T Spawn<T>(string poolName, Vector3 position = default, Quaternion rotation = default) where T : Component
        {
            var go = Instance?._poolService?.Spawn(poolName, position, rotation);
            if (go == null) return null;
            
            return Instance.GetCachedComponent<T>(go);
        }
        
        /// <summary>
        /// Returns an object to its pool.
        /// </summary>
        public static void Despawn(GameObject go)
        {
            Instance?._poolService?.Despawn(go);
        }
        
        /// <summary>
        /// Returns an object to its pool after a delay.
        /// </summary>
        public static void DespawnAfterDelay(GameObject go, float delayInSeconds)
        {
            Instance?._poolService?.DespawnAfterDelay(go, delayInSeconds);
        }
        
        /// <summary>
        /// Gets the pool bin for a prefab.
        /// </summary>
        public static IPoolBin GetPoolBin(GameObject prefab)
        {
            return Instance?._poolService?.GetPoolBin(prefab);
        }
        
        /// <summary>
        /// Gets the pool bin by name.
        /// </summary>
        public static IPoolBin GetPoolBin(string poolName)
        {
            return Instance?._poolService?.GetPoolBin(poolName);
        }
        
        /// <summary>
        /// Registers a new pool at runtime.
        /// </summary>
        public static void RegisterPool(PoolConfig config)
        {
            Instance?._poolService?.RegisterPool(config);
        }
        
        /// <summary>
        /// Unregisters a pool.
        /// </summary>
        public static void UnregisterPool(string poolName, bool destroyObjects = true)
        {
            Instance?._poolService?.UnregisterPool(poolName, destroyObjects);
        }
        
        /// <summary>
        /// Pre-warms all pools asynchronously. Call during loading screens.
        /// </summary>
        public static Coroutine WarmupAsync(Action onComplete = null)
        {
            return Instance?.StartCoroutine(Instance.WarmupCoroutine(onComplete));
        }
        
        /// <summary>
        /// Pre-warms specific pools asynchronously.
        /// </summary>
        public static Coroutine WarmupAsync(IEnumerable<PoolConfig> configs, Action onComplete = null)
        {
            return Instance?.StartCoroutine(Instance.WarmupCoroutine(configs, onComplete));
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeService()
        {
            // Initialize native lookup
            _nativeLookup = new NativePoolLookup(pools.Count > 0 ? pools.Count : 16);
            
            _poolService = new PoolService(
                transform,
                StartCoroutine,
                null
            );
            
            // Register all configured pools
            int index = 0;
            foreach (var config in pools)
            {
                if (config?.Prefab != null)
                {
                    _poolService.RegisterPool(config);
                    _nativeLookup.Register(config.Prefab.GetInstanceID(), config.Prefab.name, index++);
                }
            }
            
            if (cullInterval > 0)
            {
                _cullCoroutine = StartCoroutine(CullLoop());
            }
        }
        
        private IEnumerator CullLoop()
        {
            var wait = new WaitForSeconds(cullInterval);
            
            while (true)
            {
                yield return wait;
                _poolService.CullAllPools();
            }
        }
        
        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            if (string.IsNullOrEmpty(oldScene.name))
                return;
            
            _poolService.HandleSceneChange();
            _componentCache.Clear(); // Clear cache on scene change
        }
        
        private T GetCachedComponent<T>(GameObject go) where T : Component
        {
            int instanceId = go.GetInstanceID();
            var type = typeof(T);
            
            // Check cache first
            if (_componentCache.TryGetValue(instanceId, out var typeCache))
            {
                if (typeCache.TryGetValue(type, out var cached))
                    return (T)cached;
            }
            else
            {
                typeCache = new Dictionary<Type, Component>();
                _componentCache[instanceId] = typeCache;
            }
            
            // Cache miss - get and store
            var component = go.GetComponent<T>();
            if (component != null)
                typeCache[type] = component;
            
            return component;
        }
        
        private IEnumerator WarmupCoroutine(Action onComplete)
        {
            foreach (var config in pools)
            {
                if (config?.Prefab != null)
                {
                    var bin = _poolService.GetPoolBin(config.Prefab);
                    // Async bins will preallocate over frames
                    yield return null; // Yield each frame to spread load
                }
            }
            onComplete?.Invoke();
        }
        
        private IEnumerator WarmupCoroutine(IEnumerable<PoolConfig> configs, Action onComplete)
        {
            foreach (var config in configs)
            {
                if (config?.Prefab != null)
                {
                    _poolService.RegisterPool(config);
                    yield return null;
                }
            }
            onComplete?.Invoke();
        }
        
        #endregion
        
        #region Editor Support
        
        internal List<PoolConfig> EditorPools => pools;
        
        #endregion
    }
}
