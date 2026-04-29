namespace DummyApp.Model;

public class Test
{
    private int _count = 0;

    public bool FieldBreakingEncapsulation = true;
    
    public bool PropBreakingEncapsulation { get; set; }

    private readonly object _lock = new();
    private readonly object _anotherLock = new();
    
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
}