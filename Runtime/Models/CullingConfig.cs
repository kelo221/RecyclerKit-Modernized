using System;
using UnityEngine;

namespace RecyclerKit
{
    /// <summary>
    /// Configuration for pool culling behavior.
    /// Serializable for Unity Inspector.
    /// </summary>
    [Serializable]
    public class CullingConfig : ICullingConfig
    {
        [SerializeField]
        [Tooltip("If true, excess instances will be destroyed periodically")]
        private bool enabled;
        
        [SerializeField]
        [Tooltip("Number of instances to keep in the pool after culling")]
        private int maintainCount = 5;
        
        [SerializeField]
        [Tooltip("Interval in seconds between cull checks")]
        private float interval = 10f;
        
        public bool Enabled => enabled;
        public int MaintainCount => maintainCount;
        public float Interval => interval;
        
        public CullingConfig() { }
        
        public CullingConfig(bool enabled, int maintainCount, float interval)
        {
            this.enabled = enabled;
            this.maintainCount = Mathf.Max(0, maintainCount);
            this.interval = Mathf.Max(0f, interval);
        }
    }
}
