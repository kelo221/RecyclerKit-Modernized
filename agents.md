# RecyclerKit - LLM Agent Guide

## Purpose
Object pooling system for Unity 6. Use instead of `Instantiate`/`Destroy` for frequently spawned objects.

## IMPORTANT: Required Setup
**Every pooled prefab MUST have a `PooledObject` component attached.**

```csharp
// On prefab root, add PooledObject component
// Optional: Assign behaviors/rigidbodies/colliders to disable when pooled
```

## When to Use
- Bullets, projectiles
- Particle effects
- Enemies, NPCs
- UI elements
- Any frequently instantiated/destroyed objects

## Core API

```csharp
using RecyclerKit;

// SPAWN - Get object from pool
GameObject obj = PoolManager.Spawn(prefab, position, rotation);
GameObject obj = PoolManager.Spawn("PrefabName", position, rotation);

// Generic spawn (cached component, avoids GetComponent)
var bullet = PoolManager.Spawn<Bullet>(prefab, position, rotation);

// DESPAWN - Return to pool (zero-allocation via PooledObject)
PoolManager.Despawn(obj);
PoolManager.DespawnAfterDelay(obj, seconds);

// GET POOL INFO
IPoolBin bin = PoolManager.GetPoolBin(prefab);
int available = bin.AvailableCount;
int spawned = bin.SpawnedCount;
```

## Runtime Pool Registration

```csharp
// Basic
PoolManager.RegisterPool(new PoolConfig(prefab));

// With options (fluent API)
var config = new PoolConfig(prefab)
    .WithPreallocate(10)      // Pre-create 10 instances
    .WithGrowCount(5)         // Create 5 when empty
    .WithHardLimit(100)       // Max 100 instances
    .WithAutoRecycleParticles() // Auto-despawn particles
    .WithPersistence()        // Survive scene changes
    .WithLayerHiding(30)      // GPU Resident Drawer mode
    .WithAsyncInstantiation(); // Unity 6 async

PoolManager.RegisterPool(config);

// Remove pool
PoolManager.UnregisterPool("PrefabName");
```

## Events

```csharp
var bin = PoolManager.GetPoolBin(prefab);
bin.OnSpawned += go => { /* called on spawn */ };
bin.OnDespawned += go => { /* called on despawn */ };
```

## Hiding Methods

| Method | Code | Notes |
|:--|:--|:--|
| Layer (default) | `.WithLayerHiding(30)` | GPU optimized |
| SetActive | `.WithSetActiveHiding()` | Legacy |
| Position | `.WithPositionHiding()` | Keeps scripts active |

## Common Patterns

### Projectile
```csharp
var bullet = PoolManager.Spawn(bulletPrefab, firePoint.position, firePoint.rotation);
bullet.GetComponent<Rigidbody>().velocity = firePoint.forward * speed;
// Despawn on hit or after lifetime
```

### Particle Effect
```csharp
// Auto-recycle after particle duration
var config = new PoolConfig(particlePrefab).WithAutoRecycleParticles();
PoolManager.RegisterPool(config);

// Just spawn, it auto-despawns
PoolManager.Spawn(particlePrefab, hitPoint, Quaternion.identity);
```

### Enemy
```csharp
var enemy = PoolManager.Spawn(enemyPrefab, spawnPoint, Quaternion.identity);
enemy.GetComponent<Health>().Reset();
// On death:
PoolManager.Despawn(enemy);
```

## Advanced API

### Generic Spawn (avoids GetComponent)
```csharp
// Returns component directly - cached, no GetComponent overhead
var bullet = PoolManager.Spawn<Bullet>(bulletPrefab, pos, rot);
var enemy = PoolManager.Spawn<EnemyController>("Enemy", spawnPoint, rot);
```

### Warmup (loading screen)
```csharp
// During loading - preallocs all pools async
PoolManager.WarmupAsync(onComplete: () => Debug.Log("Pools ready"));

// Warmup specific pools
var configs = new[] { bulletConfig, particleConfig };
PoolManager.WarmupAsync(configs, onComplete: StartGame);
```

### IPoolable Interface (zero-overhead callbacks)
```csharp
public class Bullet : MonoBehaviour, IPoolable
{
    public void OnSpawn()
    {
        // Reset state when spawned
        damage = baseDamage;
        rb.velocity = Vector3.zero;
    }
    
    public void OnDespawn()
    {
        // Cleanup before returning to pool
        trail.Clear();
    }
}
```

### Per-Object Events (UnityEvents)
Configure in Inspector on `PooledObject` component:
- `onSpawn` - called when spawned
- `onDespawn` - called before despawn

## Important Notes

1. **PooledObject required** - every pooled prefab needs this component
2. **Null check** when using hard limits - `Spawn()` returns null if limit reached
3. **IPoolable** - implement for code-based spawn/despawn callbacks (zero GC)
4. **Layer 30** excluded from camera culling mask for layer hiding
5. **Prefab names** must be unique - used as pool identifiers

