using System.Diagnostics.CodeAnalysis;
using Annotations;

namespace DummyApp.Src;

#pragma warning disable CS0414 // Field is assigned but its value is never used
[SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]

[ThreadSafe]
public class Visibility
{
    private readonly object _lock = new();
    private readonly object _anotherLock = new();
    
    public int _transactions = 0;
    public readonly int _transactionReadOnly = 1;
    
    private int _count = 0;
    private readonly int _countReadOnly = 1;

    
    public bool PublicFieldBreakingEncapsulation = true;
    
    private bool _privateFieldBreakingEncapsulation = false;
    
    
    public bool PublicPropBreakingEncapsulation { get; set; }
    private bool PrivatePropBreakingEncapsulation { get; set; }
    
    // -----------------------------
    // ---------- METHODS ----------
    // -----------------------------
    // Should not Flag warnings 
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

    // Should not Flag warnings 
    public int GetCountLocked()
    {
        lock (_lock)
        {
            return _count;
        }
    }
    
    // Should Flag warnings (inconsistent lock usage) 
    public void SetCountLocked(int count)
    {
        _count = count;
    }
    
    // Should Flag warnings (no lock usage) 
    public int GetCount()
    { 
        return _count;
    }
    
    // Should Flag warnings (no lock usage) 
    public void SetCount(int count)
    {
        _count = count;
    }   
    
    // Should Flag warnings 
    public int GetCountReadOnly()
    {
        return _countReadOnly;
    }
    
    // Should Flag warnings
    public int GetTransactions()
    { 
        return _transactions;
    }
    
    public void SetTransactions(int count)
    {
        _transactions = count;
    }
    
    // Should Flag warnings (returns sync obj)
    public object GetSyncObject()
    {
        //TODO: What should we do when two rules are violated on the same location?
        return _anotherLock;
    }
    
}