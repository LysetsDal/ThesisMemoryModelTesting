using System.Threading;
using System.Threading.Tasks;
using Annotations;

namespace DummyApp.Src;

// [ThreadSafe]
public class CorrectlySynchronized
{
    private int Count { get; set; } = 0;

    private int _otherWork = 1;

    private readonly object _csLock = new();
    private readonly object _csLock2 = new();
    
    public int GetOtherWork()
    {
        return _otherWork;
    }    
    
    public void SetOtherWork(int otherWork)
    {
        _otherWork = otherWork;
    }


    public void BadLockingOnClassInstance()
    {
        lock (this)
        {
            var tmp = GetOtherWork();
            SetOtherWork(tmp + 1);
        }
    }
    

    // ====================================
    // ============== Threads =============
    // ====================================
    // Should flag a warning on both tread bodies
    public void TwoLockedThreads_DifferentLockSymbols()
    {
        var t1 = new Thread(() =>
        {
            lock (_csLock2)
            {
                var tmp = GetOtherWork();
            }
        });
        
        var t2 = new Thread(() =>
        {
            lock (_csLock)
            {
                SetOtherWork(42);
            }
        });
    }    
    
    // Should flag a warning on both tread bodies
    public void TwoLockedThreads_OneLockSymbols()
    {
        var t1 = new Thread(() =>
        {
                var tmp = GetOtherWork();
        });
        
        var t2 = new Thread(() =>
        {
            lock (_csLock)
            {
                SetOtherWork(42);
            }
        });
    }    
    
    // Should not flag a warning
    public void TwoLockedThreads_SameLockSymbols()
    {
        var t3 = new Thread(() =>
        {
            lock (_csLock)
            {
                var tmp = GetOtherWork();
            }
        });
        
        var t4 = new Thread(() =>
        {
            lock (_csLock)
            {
                SetOtherWork(42);
            }
        });
    }
    
    // ====================================
    // =============== TASKS ==============
    // ====================================
    
    
    // Should flag warning
    public void TwoLockedTasks_DifferentLockSymbols()
    {
        var t1 = new Task(() =>
        {
            lock (_csLock2)
            {
                var tmp = GetOtherWork();
            }
        });
        
        var t2 = new Task(() =>
        {
            lock (_csLock)
            {
                SetOtherWork(42);
            }
        });
    } 
    
    // Should flag a warning on both tread bodies
    public void TwoLockedTasks_OneLockSymbols()
    {
        var t1 = new Task(() =>
        {
            var tmp = GetOtherWork();
        });
        
        var t2 = new Task(() =>
        {
            lock (_csLock)
            {
                SetOtherWork(42);
            }
        });
    }   
    
    // Should not flag a warning
    public void TwoLockedTasks_SameLockSymbols()
    {
        var t3 = new Task(() =>
        {
            lock (_csLock)
            {
                var tmp = GetOtherWork();
            }
        });
        
        var t4 = new Task(() =>
        {
            lock (_csLock)
            {
                SetOtherWork(42);
            }
        });
    }
}