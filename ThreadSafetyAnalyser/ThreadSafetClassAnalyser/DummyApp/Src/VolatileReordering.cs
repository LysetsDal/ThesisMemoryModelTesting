using System.Threading;
using Annotations;
// ReSharper disable CheckNamespace

namespace DummyApp.Src;

[ThreadSafe]
public class VolatileReordering
{
    private static readonly object Lock = new ();
    private volatile int _x = 1;
    private volatile int _y = 42;
    private static int _res1;
    private static int _res2;

    private void TestVolatileReordering(int a)
    {
        _y = a;
        // Should flag VolatileReordering
        var tmp = _x;
        _res2 = tmp;
    }
    
    // Flags internal field no lock?
    private void TestVolatileReorderingWithMemoryBarrier()
    {
        _x = 2;
        Thread.MemoryBarrier();
        // Should not flag volatile reordering on _y.
        var tmp = _y;
        _res1 = tmp;
    }
    
    private void TestVolatileReorderingInsideLock()
    {
        lock (Lock)
        {
            _y = 42;
            // should not flag on _x with a lock
            var tmp = _x;
        }
    }
    
    // NOT IMPLEMENTED YET: (should not flag on _x with a monitor)
    private void TestVolatileReorderingSurroundedByMonitor()
    {
        Monitor.Enter(Lock);
        _y = 42;
        var tmp = _x;
        Monitor.Exit(Lock);
    }
}