using System.Threading;
using Annotations;

namespace DummyApp.Src;

[ThreadSafe]
public class InterlockedExample
{
    private int _counter = 0;
    private int _state = 0;

    /// <summary>
    /// TEST 1: Internal synchronization via Interlocked.
    /// EXPECTED: No warning. 
    /// Your helper should find Interlocked.Increment and mark this as safe.
    /// </summary>
    public void SafeIncrement()
    {
        Interlocked.Increment(ref _counter);
    }

    /// <summary>
    /// TEST 2: Internal synchronization via Volatile and CompareExchange.
    /// EXPECTED: No warning.
    /// Your helper should find Interlocked.CompareExchange and mark this as safe.
    /// </summary>
    public void SafeExchange(int newValue)
    {
        int current = Volatile.Read(ref _state);
        Interlocked.CompareExchange(ref _state, newValue, current);
    }

    /// <summary>
    /// TEST 3: Actual Thread-Safety Violation.
    /// EXPECTED: 'InternalFieldNoLockRule' (or similar).
    /// There is no lock and no Interlocked call here.
    /// </summary>
    public void UnsafeIncrement()
    {
        _counter++; 
    }
}

public class CallSiteTester
{
    private readonly InterlockedExample _example = new();

    /// <summary>
    /// TEST 4: Calling an internally synchronized method.
    /// EXPECTED: No 'FieldDoesNotUseLockRule' warning.
    /// AnalyzeCallingMemberAccessWithLock should look inside SafeIncrement, 
    /// see the Interlocked call, and decide it is safe to call without a lock.
    /// </summary>
    public void ValidCallSite()
    {
        _example.SafeIncrement();
    }

    /// <summary>
    /// TEST 5: Calling an unsafe method from outside.
    /// EXPECTED: 'FieldDoesNotUseLockRule'.
    /// Since UnsafeIncrement has no internal synchronization, calling it 
    /// without a lock should be flagged at this call-site.
    /// </summary>
    public void InvalidCallSite()
    {
        _example.UnsafeIncrement();
    }
}