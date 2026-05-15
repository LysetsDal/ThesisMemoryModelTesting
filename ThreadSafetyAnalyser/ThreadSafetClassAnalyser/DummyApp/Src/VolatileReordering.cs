using System.Threading;
using Annotations;

namespace DummyApp.Src;

[ThreadSafe]
public class VolatileReordering
{
    private static readonly object Lock = new ();
    private volatile int _x = 1;
    private volatile int _y = 42;
    private static int _res1;
    private static int _res2;

    // Should flag VolatileReordering
    private void TestVolatileReordering(int a)
    {
        _y = a;
        var tmp = _x;
        _res2 = tmp;
    }
    
    // Should not flag volatile reordering on _y. (Flags internal field no lock).
    private void TestVolatileReorderingWithMemoryBarrier()
    {
        _x = 2;
        Thread.MemoryBarrier();
        var tmp = _y;
        _res1 = tmp;
    }
    
    // should not flag on _x with a lock
    private void TestVolatileReorderingInsideLock()
    {
        lock (Lock)
        {
            _y = 42;
            var tmp = _x;
        }
    }
    
    // should not flag on _x with a monitor
    private void TestVolatileReorderingSurroundedByMonitor()
    {
        Monitor.Enter(Lock);
        _y = 42;
        var tmp = _x;
        Monitor.Exit(Lock);
    }
}