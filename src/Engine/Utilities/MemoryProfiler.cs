using System;

namespace MiniEngine.Utilities
{
    /// <summary>
    /// Profiles memory usage of a thread. Not thread safe, so only use on 1 thread at a time.
    /// </summary>
    public static class MemoryProfiler
    {
        private static long startBytes = 0;
        private static long endBytes = 0;
        
        public static void Begin()
        {
            startBytes = GC.GetAllocatedBytesForCurrentThread();
        }

        public static void End()
        {
            endBytes = GC.GetAllocatedBytesForCurrentThread();
        }

        public static long GetAllocatedBytes()
        {
            return endBytes - startBytes;
        }
    }
}