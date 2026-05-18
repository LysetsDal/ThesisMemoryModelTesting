using Annotations;
// ReSharper disable CheckNamespace
// ReSharper disable UnusedVariable
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Local

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
        lock (this)
        {
            return _otherWork;
        }
    }    
    
    // Should flag ConflictingAccessRule warning
    public void SetOtherWork(int otherWork)
    {
        lock (this)
        {
            _otherWork = otherWork;
        }
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
        var thread1 = new Thread(() =>
        {
            lock (_csLock2)
            {
                var tmp = GetOtherWork();
            }
        });
        
        // should flag ConflictingAccessThread warning
        var thread2 = new Thread(() =>
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
        var thread3 = new Thread(() =>
        {
                var tmp = GetOtherWork();
        });
        
        // should flag ConflictingAccessThread warning
        var thread4 = new Thread(() =>
        {
            lock (_csLock)
            {
                SetOtherWork(42);
            }
        });
    }    
    
    // Should flag ConflictingAccessRule warning
    public void TwoLockedThreads_SameLockSymbols()
    {
        // should not flag a warning
        var thread5 = new Thread(() =>
        {
            lock (_csLock)
            {
                var tmp = GetOtherWork();
            }
        });
        
        // should not flag a warning
        var thread6 = new Thread(() =>
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
        var task1 = new Task(() =>
        {
            lock (_csLock2)
            {
                var tmp = GetOtherWork();
            }
        });
        
        // should flag ConflictingAccessThread warning
        var task2 = new Task(() =>
        {
            lock (_csLock)
            {
                SetOtherWork(42);
            }
        });
    } 
    
    // Should flag ConflictingAccessRule warning
    public void TwoLockedTasks_OneLockSymbols()
    {
        // should flag ConflictingAccessThread warning
        var task3 = new Task(() =>
        {
            var tmp = GetOtherWork();
        });
        
        // should flag ConflictingAccessThread warning
        var task4 = new Task(() =>
        {
            lock (_csLock)
            {
                SetOtherWork(42);
            }
        });
    }   
    
    // Should flag ConflictingAccessRule warning
    public void TwoLockedTasks_SameLockSymbols()
    {
        var task5 = new Task(() =>
        {
            lock (_csLock)
            {
                var tmp = GetOtherWork();
            }
        });
        
        var task6 = new Task(() =>
        {
            lock (_csLock)
            {
                SetOtherWork(42);
            }
        });
    }
}