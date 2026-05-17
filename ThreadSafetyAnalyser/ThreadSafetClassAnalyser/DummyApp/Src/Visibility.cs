using System.Diagnostics.CodeAnalysis;
using Annotations;
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToConstant.Global
// ReSharper disable ConvertToConstant.Local
// ReSharper disable CheckNamespace

namespace DummyApp.Src;

#pragma warning disable CS0414 // Field is assigned but its value is never used
[SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]

// [ThreadSafe]
public class Visibility
{
    private readonly object _lock = new();
    private readonly object _anotherLock = new();
    
    // Should flag PublicFieldExposed warning
    public int _transactions = 0;
    // Should flag PublicFieldExposed warning
    public readonly int _transactionReadOnly = 1;
    
    private int _count = 0;
    private readonly int _countReadOnly = 1;
    
    // Should flag PublicFieldExposed warning
    public bool PublicFieldBreakingEncapsulation = true;
    private bool _privateFieldBreakingEncapsulation = false;
    
    // Should flag PublicFieldExposed warning
    public bool PublicPropBreakingEncapsulation { get; set; }
    private bool PrivatePropBreakingEncapsulation { get; set; }
    public bool PrivateReadonlyProp { get; } = true;
    
    public byte Slot { get; private set; }
    
    // Needed so _anotherLock is seen as a lock target
    public void UseAnotherLock()
    {
        lock (_anotherLock)
        {
            _count++;
        }
    }
    
    // --------- WARNINGS ----------
    
    // Should not Flag warnings 
    public int PublicPropWithSynchronization
    {
        get
        {
            lock (_lock)
            {
                return _count;
            }
        }
        set
        {
            lock (_lock)
            {
                _count = value;
            } 
        }
    }

    // Should not Flag warnings 
    public int GetCountLocked()
    {
        lock (_lock)
        {
            // Should flag InconsistentLockUse warning
            return _count;
        }
    }
    
    // Should Flag warnings (inconsistent lock usage) 
    public void SetCountLocked(int count)
    {
        lock (_anotherLock) 
            _count = count;
    }
    
    public int GetCount()
    { 
        // Should flag InternalFieldNoLock warning
        return _count;
    }
    
    public void SetCount(int count)
    {
        // Should flag InternalFieldNoLock warning
        _count = count;
    }   
    
    // Should Flag warnings 
    public int GetCountReadOnly()
    {
        return _countReadOnly;
    }
    
    public int GetTransactions()
    { 
        // Should flag InternalFieldNoLock warning
        return _transactions;
    }
    
    public void SetTransactions(int count)
    {
        // Should flag InternalFieldNoLock warning
        _transactions = count;
    }
    
    public object GetSyncObject()
    {
        // Should flag LockObjectExposed warning
        return _anotherLock;
    }
    
}