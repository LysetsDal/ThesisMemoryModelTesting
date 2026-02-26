using Xunit;
using Xunit.Abstractions;

namespace MemoryModelTests.Volatiles;

public class AcquireSemantics(ITestOutputHelper testOutputHelper)
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    private int _normalInt1;
    private int _normalInt2;
    private volatile bool _volatileFlag;
    private bool acquireRead;

    private bool t1SawT2StoreEarly;

    [Fact]
    public void Test_VolatileRead_Prevents_Subsequent_Load_From_Hoisting()
    {
        var N = 100_000;

        for (var i = 0; i < N; i++)
        {
            _normalInt1 = 0;
            _normalInt2 = 0;
            _volatileFlag = false;

            // Thread A: The Writer (Release)
            var t1 = new Thread(() =>
            {
                _normalInt1 = 42;
                _normalInt2 = 69;
                _volatileFlag = true; // Store (Release)
            });

            var failedAcquire = false;
            var observedData = 0;

            // Thread B: The Reader (Acquire)
            var t2 = new Thread(() =>
            {
                var acquireRead = _volatileFlag; // Load (Acquire) 
                observedData = _normalInt2;

                if (acquireRead && observedData == 0)
                {
                    failedAcquire = true;
                }
            });

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();

            Assert.False(failedAcquire,
                $"Acquire fence failed on iteration {i}. Read of _normalInt2 in t2 moved above volatile read of _volatileFlag.");
        }
    }

    [Fact]
    public void Test_VolatileRead_Prevents_Subsequent_Store_From_Hoisting()
    {
        var N = 100_000;

        for (var i = 0; i < N; i++)
        {
            _volatileFlag = false;
            _normalInt2 = 0;
            t1SawT2StoreEarly = false;
            acquireRead = false;

            var t1 = new Thread(() =>
            {
                // If t2 hoists the store, t1 might see _normalInt2 == 42 
                // while _volatileFlag is still false.
                if (_normalInt2 == 42 && !_volatileFlag)
                {
                    t1SawT2StoreEarly = true;
                }

                _volatileFlag = true;
            });

            var t2 = new Thread(() =>
            {
                acquireRead = _volatileFlag; // Load (Acquire)
                _normalInt2 = 42; // Subsequent Store
            });

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();

            // If Acquire semantics hold, t2's store to _normalInt2 CANNOT 
            // happen until it has done the load of _volatileFlag.
            Assert.False(t1SawT2StoreEarly,
                "The store in t2, was == 42 before the volatileFlag was still false. \n" +
                "That means t1SawT2StoreEarly was: ");
        }
    }
}