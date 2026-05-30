namespace ThreadSafetClassAnalyser.Utils
{
    public static class KnownTypes
    {
        public const string Task = "Task";
        public const string Thread = "Thread";
        public const string Volatile = "Volatile";
        public const string VolatileReadOp = "VolatileRead";
        public const string VolatileWriteOp = "VolatileWrite";
        public const string Interlocked = "Interlocked";
        public const string MemoryBarrier = "MemoryBarrier";
        
        public const string FullTaskName = "System.Threading.Tasks.Task";
        public const string FullThreadName = "System.Threading.Thread";
        public const string FullVolatileName = "System.Threading.Volatile";
        public const string FullInterlockedName = "System.Threading.Interlocked";
        public const string FullMonitorName = "System.Threading.Monitor";
        public const string FullSemaphoreSlimName = "System.Threading.SemaphoreSlim";
        
        // Custom Attribute Names
        public const string ThreadSafe = "Annotations.ThreadSafeAttribute";
        public const string ThreadSafeShort = "ThreadSafeAttribute";
    }
}