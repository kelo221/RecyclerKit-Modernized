namespace RecyclerKit
{
    /// <summary>
    /// Configuration for pool culling behavior.
    /// </summary>
    public interface ICullingConfig
    {
        /// <summary>
        /// Whether culling is enabled for this pool.
        /// </summary>
        bool Enabled { get; }
        
        /// <summary>
        /// Number of instances to maintain in the pool after culling.
        /// </summary>
        int MaintainCount { get; }
        
        /// <summary>
        /// Interval in seconds between cull checks.
        /// </summary>
        float Interval { get; }
    }
}
