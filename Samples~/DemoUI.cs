using UnityEngine;
using UnityEngine.UI;
using RecyclerKit;

/// <summary>
/// Demo script showing PoolManager usage patterns.
/// Updated from legacy TrashMan API.
/// </summary>
public class DemoUI : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject cubePrefab;
    public GameObject spherePrefab;
    public GameObject capsulePrefab;
    
    private bool _didCreateCapsulePool;
    private bool _didCreateUiStuff;
    private GameObject _canvasRoot;
    private GameObject _uiPrefab;
    
    private void Start()
    {
        // Subscribe to spawn/despawn events
        var cubeBin = PoolManager.GetPoolBin(cubePrefab);
        if (cubeBin != null)
        {
            cubeBin.OnSpawned += go => Debug.Log($"Spawned: {go}");
            cubeBin.OnDespawned += go => Debug.Log($"Despawned: {go}");
        }
    }
    
    private void OnGUI()
    {
        if (GUILayout.Button("Spawn Cube"))
        {
            var obj = PoolManager.Spawn(cubePrefab, Random.onUnitSphere * 5f, Random.rotation);
            PoolManager.DespawnAfterDelay(obj, Random.Range(1f, 2f));
        }
        
        if (GUILayout.Button("Spawn Sphere"))
        {
            var obj = PoolManager.Spawn(spherePrefab, Random.onUnitSphere * 3f, Quaternion.identity);
            
            // Spheres have a hard limit, null check required
            if (obj != null)
            {
                obj.transform.parent = transform;
                PoolManager.DespawnAfterDelay(obj, Random.Range(5f, 8f));
            }
        }
        
        if (GUILayout.Button("Spawn Light from Scene"))
        {
            var obj = PoolManager.Spawn("light", Random.onUnitSphere * 10f, Quaternion.identity);
            
            if (obj != null)
            {
                obj.transform.parent = transform;
                PoolManager.DespawnAfterDelay(obj, Random.Range(5f, 8f));
            }
        }
        
        if (GUILayout.Button("Spawn Particles by Name"))
        {
            PoolManager.Spawn("Particles", Random.onUnitSphere * 3f, Quaternion.identity);
        }
        
        if (GUILayout.Button("Spawn UI Element"))
        {
            CreateCanvas();
            var go = PoolManager.Spawn(_uiPrefab, Vector3.zero, Quaternion.identity);
            go.transform.SetParent(_canvasRoot.transform, true);
            
            var rt = go.transform as RectTransform;
            rt.anchoredPosition = new Vector2(Random.Range(-380, 380), Random.Range(-280, 280));
            
            PoolManager.DespawnAfterDelay(go, Random.Range(1f, 5f));
        }
        
        if (GUILayout.Button("Create Pool at Runtime"))
        {
            _didCreateCapsulePool = true;
            
            // Fluent builder pattern for config
            var config = new PoolConfig(capsulePrefab)
                .WithPreallocate(3)
                .WithGrowCount(2);
            
            PoolManager.RegisterPool(config);
        }
        
        if (_didCreateCapsulePool && GUILayout.Button("Spawn Capsule"))
        {
            var obj = PoolManager.Spawn(capsulePrefab, Random.onUnitSphere * 5f, Random.rotation);
            PoolManager.DespawnAfterDelay(obj, Random.Range(1f, 5f));
        }
    }
    
    private void CreateCanvas()
    {
        if (_didCreateUiStuff) return;
        _didCreateUiStuff = true;
        
        // Create UI canvas
        _canvasRoot = new GameObject("Canvas");
        var canvas = _canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        var scaler = _canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.referenceResolution = new Vector2(800, 600);
        
        // Create UI prefab
        _uiPrefab = new GameObject("UItext");
        _uiPrefab.transform.position = new Vector3(1000, 10000);
        
        // REQUIRED: Add PooledObject component for pooling
        _uiPrefab.AddComponent<PooledObject>();
        
        var txt = _uiPrefab.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource(typeof(Font), "LegacyRuntime.ttf") as Font;
        txt.text = "Pooled Text";
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.color = Color.white;
        txt.resizeTextForBestFit = true;
        
        // Register UI pool
        var config = new PoolConfig(_uiPrefab)
            .WithPreallocate(5);
        
        PoolManager.RegisterPool(config);
    }
}
