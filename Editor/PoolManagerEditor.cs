using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using RecyclerKit;

/// <summary>
/// Custom editor for PoolManager.
/// Provides drag-drop interface for adding prefabs to pools.
/// </summary>
[CustomEditor(typeof(PoolManager))]
public class PoolManagerEditor : Editor
{
    private List<bool> _foldouts;
    private PoolManager _target;
    
    private GUIStyle _boxStyle;
    private GUIStyle _binStyleEven;
    private GUIStyle _binStyleOdd;
    private GUIStyle _buttonStyle;
    
    private void OnEnable()
    {
        _target = target as PoolManager;
        
        _foldouts = new List<bool>();
        var pools = _target.EditorPools;
        if (pools != null)
        {
            for (int i = 0; i < pools.Count; i++)
                _foldouts.Add(true);
        }
        
        CleanupNullPools();
    }
    
    private void OnDisable()
    {
        DestroyStyles();
    }
    
    public override void OnInspectorGUI()
    {
        // Draw default inspector for basic fields
        DrawDefaultInspector();
        
        EditorGUILayout.Space(15);
        DrawDropArea();
        
        var pools = _target.EditorPools;
        if (pools == null || pools.Count == 0)
            return;
        
        // Sync foldouts count
        while (_foldouts.Count < pools.Count)
            _foldouts.Add(false);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Object Pools", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical();
        
        for (int i = 0; i < pools.Count; i++)
        {
            var pool = pools[i];
            if (pool?.Prefab == null) continue;
            
            DrawPoolConfig(pool, i, pools);
        }
        
        EditorGUILayout.EndVertical();
        
        if (GUI.changed)
            EditorUtility.SetDirty(target);
    }
    
    private void DrawPoolConfig(PoolConfig pool, int index, List<PoolConfig> pools)
    {
        EditorGUILayout.BeginVertical(index % 2 == 0 ? GetBinStyleEven() : GetBinStyleOdd());
        
        // Header with foldout and remove button
        EditorGUILayout.BeginHorizontal();
        _foldouts[index] = EditorGUILayout.Foldout(_foldouts[index], pool.Prefab.name, true);
        
        if (GUILayout.Button("X", GetButtonStyle(), GUILayout.Width(20), GUILayout.Height(15)))
        {
            if (EditorUtility.DisplayDialog("Remove Pool", 
                $"Remove pool for '{pool.Prefab.name}'?", "Yes", "Cancel"))
            {
                pools.RemoveAt(index);
                _foldouts.RemoveAt(index);
                return;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // Expanded content
        if (_foldouts[index])
        {
            EditorGUI.indentLevel++;
            
            // Use SerializedObject to access private fields properly
            var serializedPool = new SerializedObject(target);
            var poolsProperty = serializedPool.FindProperty("pools");
            
            if (poolsProperty != null && index < poolsProperty.arraySize)
            {
                var poolElement = poolsProperty.GetArrayElementAtIndex(index);
                
                DrawPoolProperty(poolElement, "preallocateCount", "Preallocate Count", 
                    "Number of instances to create at startup");
                
                DrawPoolProperty(poolElement, "growCount", "Grow Count", 
                    "Instances to create when pool is empty");
                
                DrawPoolProperty(poolElement, "autoRecycleParticles", "Auto Recycle Particles", 
                    "Auto-despawn ParticleSystems after their duration");
                
                DrawPoolProperty(poolElement, "useHardLimit", "Use Hard Limit", 
                    "Limit maximum instances");
                
                var useHardLimit = poolElement.FindPropertyRelative("useHardLimit");
                if (useHardLimit != null && useHardLimit.boolValue)
                {
                    EditorGUI.indentLevel++;
                    DrawPoolProperty(poolElement, "hardLimit", "Hard Limit", 
                        "Maximum instances allowed");
                    EditorGUI.indentLevel--;
                }
                
                // Culling section
                var culling = poolElement.FindPropertyRelative("culling");
                if (culling != null)
                {
                    DrawProperty(culling, "enabled", "Enable Culling", 
                        "Destroy excess instances periodically");
                    
                    var cullingEnabled = culling.FindPropertyRelative("enabled");
                    if (cullingEnabled != null && cullingEnabled.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        DrawProperty(culling, "maintainCount", "Maintain Count", 
                            "Keep this many instances after culling");
                        DrawProperty(culling, "interval", "Cull Interval", 
                            "Seconds between cull checks");
                        EditorGUI.indentLevel--;
                    }
                }
                
                DrawPoolProperty(poolElement, "persistBetweenScenes", "Persist Between Scenes", 
                    "Keep pool when scenes change");
                
                serializedPool.ApplyModifiedProperties();
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }
    
    private void DrawPoolProperty(SerializedProperty poolElement, string propName, string label, string tooltip)
    {
        var prop = poolElement.FindPropertyRelative(propName);
        if (prop != null)
        {
            EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip));
        }
    }
    
    private void DrawProperty(SerializedProperty parent, string propName, string label, string tooltip)
    {
        var prop = parent.FindPropertyRelative(propName);
        if (prop != null)
        {
            EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip));
        }
    }
    
    private void DrawDropArea()
    {
        var evt = Event.current;
        var dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drop Prefabs Here to Create Pools", GetBoxStyle());
        
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    break;
                
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is GameObject go)
                            AddPool(go);
                    }
                }
                
                Event.current.Use();
                break;
        }
    }
    
    private void AddPool(GameObject prefab)
    {
        var pools = _target.EditorPools;
        if (pools == null)
        {
            Debug.LogError("Cannot access pools list");
            return;
        }
        
        // Check for duplicates
        foreach (var pool in pools)
        {
            if (pool?.Prefab != null && pool.Prefab.name == prefab.name)
            {
                EditorUtility.DisplayDialog("Pool Manager", 
                    $"A pool for '{prefab.name}' already exists.", "OK");
                return;
            }
        }
        
        pools.Add(new PoolConfig(prefab));
        _foldouts.Add(true);
        EditorUtility.SetDirty(target);
    }
    
    private void CleanupNullPools()
    {
        var pools = _target.EditorPools;
        if (pools == null) return;
        
        for (int i = pools.Count - 1; i >= 0; i--)
        {
            if (pools[i]?.Prefab == null)
                pools.RemoveAt(i);
        }
    }
    
    #region Styles
    
    private GUIStyle GetBoxStyle()
    {
        if (_boxStyle == null)
        {
            _boxStyle = new GUIStyle(GUI.skin.box);
            
            var tex = new Texture2D(1, 1);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixel(0, 0, new Color(0.2f, 0.35f, 0.5f)); // Blue-ish tint
            tex.Apply();
            _boxStyle.normal.background = tex;
            _boxStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f); // Light gray text
            
            _boxStyle.fontSize = 12;
            _boxStyle.fontStyle = FontStyle.Italic;
            _boxStyle.alignment = TextAnchor.MiddleCenter;
        }
        return _boxStyle;
    }
    
    private GUIStyle GetBinStyleEven()
    {
        if (_binStyleEven == null)
        {
            _binStyleEven = new GUIStyle(GUI.skin.box);
            
            var tex = new Texture2D(1, 1);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixel(0, 0, new Color(0.25f, 0.25f, 0.25f, 0.5f));
            tex.Apply();
            _binStyleEven.normal.background = tex;
            
            _binStyleEven.padding = new RectOffset(10, 4, 4, 4);
        }
        return _binStyleEven;
    }
    
    private GUIStyle GetBinStyleOdd()
    {
        if (_binStyleOdd == null)
        {
            _binStyleOdd = new GUIStyle(GUI.skin.box);
            
            var tex = new Texture2D(1, 1);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f, 0.5f));
            tex.Apply();
            _binStyleOdd.normal.background = tex;
            
            _binStyleOdd.padding = new RectOffset(10, 4, 4, 4);
        }
        return _binStyleOdd;
    }
    
    private GUIStyle GetButtonStyle()
    {
        if (_buttonStyle == null)
        {
            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.normal.textColor = Color.red;
            _buttonStyle.fontStyle = FontStyle.Bold;
        }
        return _buttonStyle;
    }
    
    private void DestroyStyles()
    {
        if (_boxStyle?.normal.background != null)
            DestroyImmediate(_boxStyle.normal.background);
        if (_binStyleEven?.normal.background != null)
            DestroyImmediate(_binStyleEven.normal.background);
        if (_binStyleOdd?.normal.background != null)
            DestroyImmediate(_binStyleOdd.normal.background);
        
        _boxStyle = null;
        _binStyleEven = null;
        _binStyleOdd = null;
        _buttonStyle = null;
    }
    
    #endregion
}
