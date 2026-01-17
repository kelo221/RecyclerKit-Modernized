using System;
using Unity.Collections;
using UnityEngine;

namespace RecyclerKit
{
    /// <summary>
    /// Native collection wrapper for zero-GC pool lookups.
    /// Uses NativeParallelHashMap for O(1) lookups without managed allocations.
    /// </summary>
    public struct NativePoolLookup : IDisposable
    {
        private NativeParallelHashMap<int, int> _instanceIdToIndex;
        private NativeParallelHashMap<int, int> _nameHashToInstanceId;
        private bool _isCreated;
        
        public bool IsCreated => _isCreated;
        
        /// <summary>
        /// Creates a native lookup with specified capacity.
        /// </summary>
        /// <param name="capacity">Initial capacity for pools.</param>
        /// <param name="allocator">Memory allocator (Persistent for long-lived pools).</param>
        public NativePoolLookup(int capacity, Allocator allocator = Allocator.Persistent)
        {
            _instanceIdToIndex = new NativeParallelHashMap<int, int>(capacity, allocator);
            _nameHashToInstanceId = new NativeParallelHashMap<int, int>(capacity, allocator);
            _isCreated = true;
        }
        
        /// <summary>
        /// Registers a pool with the lookup.
        /// </summary>
        public void Register(int instanceId, string poolName, int index)
        {
            _instanceIdToIndex.TryAdd(instanceId, index);
            _nameHashToInstanceId.TryAdd(poolName.GetHashCode(), instanceId);
        }
        
        /// <summary>
        /// Unregisters a pool from the lookup.
        /// </summary>
        public void Unregister(int instanceId, string poolName)
        {
            _instanceIdToIndex.Remove(instanceId);
            _nameHashToInstanceId.Remove(poolName.GetHashCode());
        }
        
        /// <summary>
        /// Tries to get pool index by prefab instance ID.
        /// </summary>
        public bool TryGetIndex(int instanceId, out int index)
        {
            return _instanceIdToIndex.TryGetValue(instanceId, out index);
        }
        
        /// <summary>
        /// Tries to get prefab instance ID by pool name.
        /// </summary>
        public bool TryGetInstanceId(string poolName, out int instanceId)
        {
            return _nameHashToInstanceId.TryGetValue(poolName.GetHashCode(), out instanceId);
        }
        
        /// <summary>
        /// Checks if a pool exists by instance ID.
        /// </summary>
        public bool ContainsId(int instanceId)
        {
            return _instanceIdToIndex.ContainsKey(instanceId);
        }
        
        /// <summary>
        /// Checks if a pool exists by name.
        /// </summary>
        public bool ContainsName(string poolName)
        {
            return _nameHashToInstanceId.ContainsKey(poolName.GetHashCode());
        }
        
        public void Dispose()
        {
            if (_isCreated)
            {
                _instanceIdToIndex.Dispose();
                _nameHashToInstanceId.Dispose();
                _isCreated = false;
            }
        }
    }
}
