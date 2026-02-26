namespace MemoryModelTests.Readonly;

public class UnsafeInitialization
{
    private object o;
    private int x;

    public UnsafeInitialization()
    {
        x = 42;
        o = new object();
    }

    public int readX()
    {
        return x;
    }

    public object readO()
    {
        return o;
    }
}