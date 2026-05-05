using System.Diagnostics.CodeAnalysis;
using Annotations;

namespace DummyApp.Model;

#pragma warning disable CS0414 // Field is assigned but its value is never used
[SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]

// Custom Annotation
[ThreadSafe("_lock")]
public class Test
{
    private readonly object _lock = new();
    private readonly object _anotherLock = new();
    
    public int _transactions = 0;
    private int _count = 0;

    
    public bool PublicFieldBreakingEncapsulation = true;
    
    private bool _privateFieldBreakingEncapsulation = false;
    
    
    public bool PublicPropBreakingEncapsulation { get; set; }
    
    private bool PrivatePropBreakingEncapsulation { get; set; }
    
    public int PublicPropWithSynchronization
    {
        get
        {
            lock (_lock) return _count;
        }
        set
        {
            lock (_lock) _count = value;
        }
    }
    
    public void MethodWithDoubleNestedLocks()
    {
        lock (_lock)
        {
            lock (_anotherLock)
            {
                PublicFieldBreakingEncapsulation = false;
            }
        }
    }
    
    public int GetCountLocked()
    {
        lock (_lock)
        {
            return _count;
        }
    }
    
    public void SetCountLocked(int count)
    {
        lock (_anotherLock)
        {
            _count = count;
        }
    }
    
    public int GetCount()
    { 
        return _count;
    }
    
    public void SetCount(int count)
    {
        _count = count;
    }   
    
    public int GetTransactions()
    { 
        return _transactions;
    }
    
    public void SetTransactions(int count)
    {
        _transactions = count;
    }
}