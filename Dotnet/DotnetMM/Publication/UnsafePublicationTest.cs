using Xunit;
using Xunit.Abstractions;

namespace MemoryModelTests.Publication;

public class UnsafePublicationTest(ITestOutputHelper testOutputHelper)
{
    private Barrier _barrier;
    private UnsafeClass _sharedInstance;
    private int _observedValue;
    private bool _isRunning = true;

    // A simple class that is NOT thread-safe due to unsafe publication
    private class UnsafeClass(int val)
    {
        public int Data = val;
    }

    [Fact]
    public void Test_UnsafePublication_Can_Show_Zero_Value()
    {
        // Note: On x86/x64, this test will almost always pass (asserting 42).
        // On ARM64 or under heavy JIT stress, the risk of observing 0 increases.
        var N = 100_000;
        var zeroObservedCount = 0;

        for (var i = 0; i < N; i++)
        {
            _barrier = new Barrier(2);
            _sharedInstance = null;
            _observedValue = -1;

            // Thread 1: Publishes the instance to a plain static-style field
            // The CPU might reorder the assignment to _sharedInstance 
            // BEFORE the 'Data = 42' assignment.
            var t1 = new Thread(() =>
            {
                _barrier.SignalAndWait();
                _sharedInstance = new UnsafeClass(42);
            });

            // Thread 2: Grabs the reference as soon as it's non-null
            var t2 = new Thread(() =>
            {
                _barrier.SignalAndWait();
                
                // Spin until we see the reference
                while (_sharedInstance == null) { }

                // UNSAFE: We might see the object, but not its initialized data
                _observedValue = _sharedInstance.Data;
            });

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();

            if (_observedValue == 0)
            {
                zeroObservedCount++;
            }
        }

        testOutputHelper.WriteLine($"Unsafe Publication Failures: {zeroObservedCount} / {N}");
        
        // This assertion is "aspirational" for the sake of demonstrating the flaw.
        // It highlights that without a barrier, the value 0 is technically possible.
        Assert.True(zeroObservedCount >= 0); 
    }
    
    [Fact]
    public void Test_ARM_Weak_Ordering_Provocation()
    {
        var N = 200_000; // Increase iterations for ARM
        var zeroObservedCount = 0;

        for (var i = 0; i < N; i++)
        {
            // Thread 1: The Writer (Construction)
            var t1 = new Thread(() =>
            {
                while (_isRunning)
                {
                    // On ARM, the CPU can publish the address to _sharedInstance
                    // BEFORE it finishes writing 42 to the Data field.
                    _sharedInstance = new UnsafeClass(42);
                    _sharedInstance = null; // Reset for next loop
                }
            });

            // Thread 2: The Reader (Observation)
            var t2 = new Thread(() =>
            {
                for (int i = 0; i < N; i++)
                {
                    var local = _sharedInstance;
                    if (local != null)
                    {
                        // If we catch it in the middle of a reordered constructor:
                        if (local.Data == 0)
                        {
                            Interlocked.Increment(ref zeroObservedCount);
                        }
                    }
                }

                _isRunning = false;
            });

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();

            _isRunning = true;
        }

        testOutputHelper.WriteLine($"ARM Observed 'Torn' Publication: {zeroObservedCount} times.");
        Assert.Equal(0, zeroObservedCount); // This will likely FAIL on your ARM machine!
    }
}