// ReSharper disable RedundantUsingDirective
using System.Threading;
using System.Threading.Tasks;
using Annotations;
// ReSharper disable CheckNamespace
// ReSharper disable UnusedVariable

namespace DummyApp.Src;

[ThreadSafe]
public class CorrectlySynchronized
{
    private int Count { get; set; } = 0;

    private int _otherWork = 1;

    private readonly object _csLock = new();
    private readonly object _csLock2 = new();
    
    // Should flag ConflictingAccessRule warning
    public int GetOtherWork()
    {
        return _otherWork;
    }    
    
    // Should flag ConflictingAccessRule warning
    public void SetOtherWork(int otherWork)
    {
        _otherWork = otherWork;
    }
    
    // Should flag ConflictingAccessRule warning
    public void BadLockingOnClassInstance()
    {
        // Should flag LockOnClassInstance warning
        lock (this)
        {
            var tmp = GetOtherWork();
            SetOtherWork(tmp + 1);
        }
    }

    // ====================================
    // ============== Threads =============
    // ====================================
    // Should flag ConflictingAccessRule warning
    public void TwoLockedThreads_DifferentLockSymbols()
    {
        // should flag ConflictingAccessThread warning
        var t1 = new Thread(() =>
        {
            lock (_csLock2)
            {
                var tmp = GetOtherWork();
            }
        });
        
        // should flag ConflictingAccessThread warning
        var t2 = new Thread(() =>
        {
            lock (_csLock)
            {
                SetOtherWork(42);
            }
        });
    }    
    
    // Should flag ConflictingAccessRule warning
    public void TwoLockedThreads_OneLockSymbols()
    {
        // should flag ConflictingAccessThread warning
        var t1 = new Thread(() =>
        {
                var tmp = GetOtherWork();
        });
        
        // should flag ConflictingAccessThread warning
        var t2 = new Thread(() =>
        {
            lock (_csLock)
            {
                SetOtherWork(42);
            }
        });
    }    
    
    // Should flag a ConflictingAccessRule warning
    public void TwoLockedThreads_SameLockSymbols()
    {
        // should not flag a warning
        var t3 = new Thread(() =>
        {
            lock (_csLock)
            {
                var tmp = GetOtherWork();
            }
        });
        
        // should not flag a warning
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
    
    
    // Should flag ConflictingAccessRule warning
    public void TwoLockedTasks_DifferentLockSymbols()
    {
        // should flag ConflictingAccessThread warning
        var t1 = new Task(() =>
        {
            lock (_csLock2)
            {
                var tmp = GetOtherWork();
            }
        });
        
        // should flag ConflictingAccessThread warning
        var t2 = new Task(() =>
        {
            lock (_csLock)
            {
                SetOtherWork(42);
            }
        });
    } 
    
    // Should flag a ConflictingAccessRule warning
    public void TwoLockedTasks_OneLockSymbols()
    {
        // should flag ConflictingAccessThread warning
        var t1 = new Task(() =>
        {
            var tmp = GetOtherWork();
        });
        
        // should flag ConflictingAccessThread warning
        var t2 = new Task(() =>
        {
            lock (_csLock)
            {
                SetOtherWork(42);
            }
        });
    }   
    
    // Should flag 'ConflictingAccessRule' warning
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