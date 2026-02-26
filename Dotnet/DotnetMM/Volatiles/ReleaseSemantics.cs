using Xunit;
using Xunit.Abstractions;

namespace MemoryModelTests.Volatiles;

public class ReleaseSemantics(ITestOutputHelper testOutputHelper)
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    private int _data = 0;
    private volatile bool _volatileFlag;

    [Fact]
    public void Test_VolatileWrite_Prevents_Previous_Store_From_Hoisting_Down()
    {
        var N = 100_000;

        for (var i = 0; i < N; i++)
        {
            _data = 0;
            _volatileFlag = false;

            // Thread A: The Writer
            var t1 = new Thread(() =>
            {
                _data = 42;
                _volatileFlag = true; // Store (Release)
            });

            var observedData = -1;
            var sawFlagTrue = false;

            // Thread B: The Reader (Acquire)
            var t2 = new Thread(() =>
            {
                if (_volatileFlag)
                {
                    sawFlagTrue = true;
                    observedData = _data;
                }
            });

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();

            if (sawFlagTrue)
            {
                Assert.True(observedData == 42,
                    $"Release failure at iteration {i}: Saw flag=true but data=0.");
            }
        }
    }
}