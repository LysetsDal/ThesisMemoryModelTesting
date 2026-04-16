using Xunit;
using Xunit.Abstractions;

namespace MemoryModelTests.Readonly;

public class UnsafeInitializationTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    private Barrier barrier;
    private UnsafeInitialization u;

    public UnsafeInitializationTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Test_UnsafeInitialization()
    {
        var N = 100_000;

        var count = 0;

        for (var i = 0; i < N; i++)
        {
            var t1 = new Thread(() => { u = new UnsafeInitialization(); });

            var t2 = new Thread(() =>
            {
                if (u != null && u.readX() != 42)
                {
                    _testOutputHelper.WriteLine("T2: readX(): " + u.readX());
                    count++;
                    Assert.Fail("T2: x is not equal 42");
                }
            });

            var t3 = new Thread(() =>
            {
                if (u != null && u.readO() == null)
                {
                    _testOutputHelper.WriteLine("T3: readO(): " + u.readO());
                    count++;
                    Assert.Fail("T3: o is null");
                }
            });

            t1.Start();
            t2.Start();
            t3.Start();

            t1.Join();
            t2.Join();
            t3.Join();

            u = null;
        }
        
        Assert.Equal(0, count);
    }
}