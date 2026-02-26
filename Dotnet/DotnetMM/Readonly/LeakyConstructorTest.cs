using Xunit;
using Xunit.Abstractions;

namespace MemoryModelTests.Readonly;

public class LeakyConstructorTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    private static LeakyConstructor _globalInstance;

    private Barrier _barrier;

    private int _observedData = -1;
    public LeakyConstructorTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Test_Readonly_Can_Be_Zero_During_Construction_Should_Fail()
    {
        var count = 0;
        var N = 100_000;
        for (var i = 0; i < N; i++)
        {
            _barrier = new Barrier(2);
            _globalInstance = null;
            _observedData = -1;

            // Thread 1: Constructs the object
            var t1 = new Thread(() =>
            {
                _barrier.SignalAndWait();
                _globalInstance = new LeakyConstructor(42);
            });

            // Thread 2: Tries to read as the reference is visible
            var t2 = new Thread(() =>
            {
                _barrier.SignalAndWait();

                while (_globalInstance == null) { }

                _observedData = _globalInstance.Data;
            });

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();

            // If we catch it, _observedData will be 0
            if (_observedData == 0)
            {
                // Assert.Fail($"FAILURE: Readonly field was 0 at iteration {i}");
                count++;
            }
        }
        
        _testOutputHelper.WriteLine($"FAILURE: Readonly field was 0 {count} out of {N} iterations");
        Assert.NotEqual(0, count);
    }

    public class LeakyConstructor
    {
        public readonly int Data;

        public LeakyConstructor(int value)
        {
            // VIOLATION: Publish 'this' reference before the date field is set
            // and the ctor finishes
            _globalInstance = this;

            Data = value;
        }
    }
}