# RecyclerKit (Modernized)

**RecyclerKit** is a lightweight, high-performance object pooling system for Unity. This version is a modernized fork of [prime31's RecyclerKit](https://github.com/prime31/RecyclerKit), rebuilt for modern Unity development standards.

Object pooling is essential for maintaining high frame rates and minimizing Garbage Collector (GC) spikes by reusing objects instead of constantly calling `Instantiate` and `Destroy`.

## What's Modernized?
This fork brings the classic utility into the modern era:

- **Unity 6.3 LTS Optimized**: Fully compatible with Unity 6 (6000.3.x) and modern C# features.
- **UPM Support**: Now a proper Unity Package. Install and update via Git URL.
- **Assembly Definitions**: Includes `RecyclerKit.asmdef` and `RecyclerKit.Editor.asmdef` to ensure faster compilation times and clean dependency separation.
- **Clean Structure**: Scripts have been moved to standard `Runtime` and `Editor` folders, removing project clutter.
- **Enhanced Performance**: Refactored internal lookups using `NativeHashMap` (Native Collections) for better efficiency in high-frequency spawning scenarios.

## Installation

### Via Git URL
1. In Unity, open **Window > Package Manager**.
2. Click the **+** (plus) button and select **Add package from git URL...**
3. Enter the following URL:
   ```text
   https://github.com/kelo221/RecyclerKit-Modernized.git
   ```

## How It Works
RecyclerKit uses the concept of a `PoolManager` (formerly `TrashMan`). Instead of creating a new instance of a prefab, you request one from the pool.

### Basic Usage

#### 1. Prepare your Prefab
Attach the `PooledObject` component to any prefab you want to pool. This allows the system to track its state and lifecycle.

#### 2. Spawning an Object
Instead of `Instantiate(prefab)`, use `PoolManager.Spawn`:

```csharp
using RecyclerKit;

// Returns a pooled instance if available, otherwise creates one
var myObj = PoolManager.Spawn(prefab, position, rotation);

// Or spawn by pool name
var myObj2 = PoolManager.Spawn("MyPrefabName", position, rotation);
```

#### 3. Despawning an Object
Instead of `Destroy(gameObject)`, use `PoolManager.Despawn`:

```csharp
using RecyclerKit;

// Returns the object to the pool for later reuse
PoolManager.Despawn(gameObject);
```

### Advanced: Pre-warming the Pool
In Unity 6, performance during scene transitions is key. You can pre-fill your pool so there is zero allocation during gameplay. You can now do this asynchronously!

```csharp
// Pre-warm pools defined in the inspector asynchronously
StartCoroutine(PoolManager.WarmupAsync(() => {
    Debug.Log("All pools ready!");
}));
```

## Why use this in Unity 6?
While Unity has a built-in `UnityEngine.Pool` API now, **RecyclerKit** remains a favorite for developers who want:
1. **Simplicity**: A much simpler API for GameObjects than the built-in generic pool.
2. **Automatic Management**: The `PoolManager` handling the heavy lifting without needing complex boilerplate for every object type.
3. **Editor Tooling**: Includes custom inspectors to see your pools in real-time.

## License
MIT (See [LICENSE.txt](LICENSE.txt) for details)