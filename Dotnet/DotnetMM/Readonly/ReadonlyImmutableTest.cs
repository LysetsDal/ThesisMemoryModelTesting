using Xunit;

namespace MemoryModelTests.Readonly;

public class ReadonlyImmutableTest
{
    private Barrier _barrier;

    [Fact]
    public void Test_Readonly_Can_Be_Default_Value_After_Ctor_Finishes()
    {
        var N = 100_000;

        for (var i = 0; i < N; i++)
        {
            _barrier = new Barrier(2);
            ReadonlyImmutable readonlyImmutable = null;
            var observedData = -1;

            // Thread 1: Constructs the class with the readonly field
            var t1 = new Thread(() =>
            {
                _barrier.SignalAndWait();
                readonlyImmutable = new ReadonlyImmutable(1);
            });

            // Thread 2: Reads the field after object construction finishes
            var t2 = new Thread(() =>
            {
                _barrier.SignalAndWait();

                while (readonlyImmutable == null) { }

                observedData = readonlyImmutable.ReadonlyData;
            });

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();

            // If readonly semantics failed, observedData should be 0
            Assert.Equal(1, observedData);
        }
    }

    [Fact]
    public void Test_Readonly_StoreLoad_Can_Be_Reordered_And_show_Default_Value()
    {
        var N = 100_000;

        for (var i = 0; i < N; i++)
        {
            _barrier = new Barrier(2);
            ReadonlyImmutable helperObject1 = null;
            ReadonlyImmutable helperObject2 = null;
            var observedData1 = -1;
            var observedData2 = -1;


            // Thread 1: Constructs the class with the readonly field
            var t1 = new Thread(() =>
            {
                _barrier.SignalAndWait();

                helperObject1 = new ReadonlyImmutable(1);

                while (helperObject2 == null) { }

                observedData1 = helperObject2.ReadonlyData;
            });

            // Thread 2: Reads the field after object construction finishes
            var t2 = new Thread(() =>
            {
                _barrier.SignalAndWait();

                helperObject2 = new ReadonlyImmutable(2);

                while (helperObject1 == null) { }

                observedData2 = helperObject1.ReadonlyData;
            });

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();

            // If readonly semantics failed, observedData should be 0
            Assert.Equal(2, observedData1);
            Assert.Equal(1, observedData2);
        }
    }
}