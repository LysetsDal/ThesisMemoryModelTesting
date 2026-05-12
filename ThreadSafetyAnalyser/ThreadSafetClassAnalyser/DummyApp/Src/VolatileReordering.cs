using System.Threading;
using Annotations;

namespace DummyApp.Src;

// [ThreadSafe]
public class VolatileReordering
{
    private volatile int _x = 1;
    private volatile int _y = 42;
    private static int _res1;
    private static int _res2;

    private void TestVolatileReorderingA()
    {
        _x = 2;
        Thread.MemoryBarrier();
        var tmp = _y;
        _res1 = tmp;
    }

    private void TestVolatileReorderingB()
    {
        _y = 2;
        var tmp = _x;
        _res2 = tmp;
    }
}