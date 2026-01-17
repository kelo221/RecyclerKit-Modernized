namespace RecyclerKit
{
    /// <summary>
    /// Method used to hide pooled objects when despawned.
    /// </summary>
    public enum HidingMethod
    {
        /// <summary>
        /// Use SetActive(false). Compatible but expensive.
        /// Triggers OnDisable callbacks and component state changes.
        /// </summary>
        SetActive,
        
        /// <summary>
        /// Change to hidden layer. GPU Resident Drawer compatible.
        /// Object stays in GPU batch for instant re-activation.
        /// Default and recommended for Unity 6.
        /// </summary>
        Layer,
        
        /// <summary>
        /// Move position far off-screen. 
        /// Object stays active, useful for audio sources or ongoing effects.
        /// </summary>
        Position
    }
}
