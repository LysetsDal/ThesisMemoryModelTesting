namespace MemoryModelTests.Utils;


public class SyntheticVolatile
{
    
    public static int Read(ref int location)
    {
        Thread.MemoryBarrier();
        int value = Volatile.Read(ref location);
        return value;
    }
    
}