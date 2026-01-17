using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RecyclerKit
{
    /// <summary>
    /// Burst-compiled job for calculating cull amounts across many pools in parallel.
    /// Runs on worker threads to avoid main thread blocking.
    /// </summary>
    [BurstCompile]
    public struct CalculateCullAmountsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> LastCullTimes;
        [ReadOnly] public NativeArray<int> AvailableCounts;
        [ReadOnly] public NativeArray<int> MaintainCounts;
        [ReadOnly] public NativeArray<float> CullIntervals;
        [ReadOnly] public NativeArray<bool> CullingEnabled;
        
        public float CurrentTime;
        
        [WriteOnly] public NativeArray<int> CullAmounts;
        
        public void Execute(int index)
        {
            // Skip if culling disabled for this pool
            if (!CullingEnabled[index])
            {
                CullAmounts[index] = 0;
                return;
            }
            
            // Skip if not enough time has passed since last cull
            if (CurrentTime < LastCullTimes[index] + CullIntervals[index])
            {
                CullAmounts[index] = 0;
                return;
            }
            
            // Calculate excess objects to cull
            CullAmounts[index] = math.max(0, AvailableCounts[index] - MaintainCounts[index]);
        }
    }
    
    /// <summary>
    /// Helper for scheduling cull jobs.
    /// </summary>
    public static class CullJobScheduler
    {
        /// <summary>
        /// Schedules a parallel cull amount calculation job.
        /// </summary>
        /// <param name="poolCount">Number of pools.</param>
        /// <param name="lastCullTimes">Last cull time for each pool.</param>
        /// <param name="availableCounts">Available object count for each pool.</param>
        /// <param name="maintainCounts">Maintain count config for each pool.</param>
        /// <param name="cullIntervals">Cull interval for each pool.</param>
        /// <param name="cullingEnabled">Whether culling is enabled for each pool.</param>
        /// <param name="currentTime">Current Time.time value.</param>
        /// <param name="cullAmounts">Output: number of objects to cull per pool.</param>
        /// <returns>Job handle to wait on.</returns>
        public static JobHandle ScheduleCull(
            int poolCount,
            NativeArray<float> lastCullTimes,
            NativeArray<int> availableCounts,
            NativeArray<int> maintainCounts,
            NativeArray<float> cullIntervals,
            NativeArray<bool> cullingEnabled,
            float currentTime,
            NativeArray<int> cullAmounts)
        {
            var job = new CalculateCullAmountsJob
            {
                LastCullTimes = lastCullTimes,
                AvailableCounts = availableCounts,
                MaintainCounts = maintainCounts,
                CullIntervals = cullIntervals,
                CullingEnabled = cullingEnabled,
                CurrentTime = currentTime,
                CullAmounts = cullAmounts
            };
            
            // Schedule with batch size for work stealing
            return job.Schedule(poolCount, 32);
        }
    }
}
