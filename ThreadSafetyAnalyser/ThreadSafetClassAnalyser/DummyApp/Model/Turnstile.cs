using Annotations;

namespace DummyApp.Model;

[ThreadSafe]
public class Turnstile
{
    private int Count { get; set; } = 0;

    private int otherWork = 1;

    private readonly object _turnstileLock = new();
    private readonly object _turnstileLock2 = new();
    
    public int GetOtherWork()
    {
        return otherWork;
    }    
    
    public void SetOtherWork(int _otherWork)
    {
        otherWork = _otherWork;
    }

    // This method represents a turnstile spinning 1,000 times
    private void Entrance(int increments)
    {
        lock (_turnstileLock)
        {
            for (var i = 0; i < increments; i++)
            {
                // The Race Condition happens here
                Count++;
            }
        }
    }

    // Should flag a warning
    public void TwoLockedThreads_DifferentLockSymbols()
    {
        var t1 = new Thread(() =>
        {
            lock (_turnstileLock2)
            {
                var tmp = GetOtherWork();
            }
        });
        
        var t2 = new Thread(() =>
        {
            lock (_turnstileLock)
            {
                SetOtherWork(42);
            }
        });
        
    }    
    
    // Should flag a warning with TwoLockedThreads_DifferentLockSymbols()
    public void TwoLockedThreads_SameLockSymbols()
    {
        var t3 = new Thread(() =>
        {
            lock (_turnstileLock)
            {
                var tmp = GetOtherWork();
            }
        });
        
        var t4 = new Thread(() =>
        {
            lock (_turnstileLock)
            {
                SetOtherWork(42);
            }
        });
    }
}