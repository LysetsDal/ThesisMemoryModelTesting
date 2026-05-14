using Annotations;

namespace DummyApp.Src;

// [ThreadSafe]
public static partial class InternalLogger
{
    private static readonly object LockObject = new object();
    
    public static void DoWork()
    {
        // do work
        LockValidator();
        var res = -1;
    }
    
    public static void LockValidator()
    {
        lock (LockObject)
        {
            Console.WriteLine("Other work");
            var count = 0;
        }
    }
}