using Xunit;

namespace MemoryModelTests.Volatiles;

using System;
using System.Threading;


public class StoreBufferTest
{
    // Use a Barrier to align threads precisely
    private static Barrier barrier;
    
    static volatile int x;
    static volatile int y;

    static int r1, r2;
    
    static void Thread1()
    {
        barrier.SignalAndWait();
        x = 1;
        r1 = y;
    }   
    
    static void Thread1_MemoryBarrier()
    {
        barrier.SignalAndWait();
        x = 1; 
        Thread.MemoryBarrier();
        r1 = y; 
    }

    static void Thread2()
    {
        barrier.SignalAndWait();
        y = 1;
        r2 = x;
    }    
    
    static void Thread2_MemoryBarrier()
    {
        barrier.SignalAndWait();
        y = 1;
        Thread.MemoryBarrier(); 
        r2 = x;
    }
    

    [Fact]
    public void StoreBuffer_NoMemoryBarrier()
    {
        int reorderCount = 0;
        long iteration = 0;
        
        while(iteration < 100_000)
        {
            barrier = new Barrier(2);
            iteration++;
            x = 0; y = 0;
            r1 = -1; r2 = -1;
            
            Thread t1 = new Thread(Thread1);
            Thread t2 = new Thread(Thread2);
            
            
            t1.Start();
            t2.Start();
            
            t1.Join();
            t2.Join();
            

            if (r1 == 0 && r2 == 0)
            {
                reorderCount++;
            }
            
        }
        
        Assert.False(reorderCount == 0, $"Detected ${reorderCount} reorderings");
        
    }    
    
    [Fact]
    public void StoreBuffer_WithMemoryBarrier()
    {
        int reorderCount = 0;
        long iteration = 0;
        
        while(iteration < 100_000)
        {
            barrier = new Barrier(2);
            iteration++;
            x = 0; y = 0;
            r1 = -1; r2 = -1;
            
            Thread t1 = new Thread(Thread1_MemoryBarrier);
            Thread t2 = new Thread(Thread2_MemoryBarrier);
            
            
            t1.Start();
            t2.Start();
            
            t1.Join();
            t2.Join();
            

            if (r1 == 0 && r2 == 0)
            {
                reorderCount++;
            }
            
        }
        
        Assert.True(reorderCount == 0, $"Detected ${reorderCount} reorderings");
    }
}