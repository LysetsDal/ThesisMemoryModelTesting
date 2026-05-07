namespace ThreadSafetClassAnalyser.Utils
{
    public static class KnownTypes
    {
        public const string Thread = "Thread";
        public const string Task = "Task";
        
        public const string FullThreadName = "System.Threading.Thread";
        public const string FullTaskName = "System.Threading.Tasks.Task";
        
        public const string ThreadSafe = "Annotations.ThreadSafeAttribute";
        public const string ThreadSafeShort = "ThreadSafeAttribute";
    }
}