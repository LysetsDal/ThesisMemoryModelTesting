using MemoryModelTests.Readonly;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MemoryModelTests;

internal class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Starting tests");
        ITestOutputHelper testOutputHelper = new TestOutputHelper();
        var readonlyImmutable = new ReadonlyImmutableTest(testOutputHelper);
        readonlyImmutable.Test_Readonly_Can_Be_Default_Value_After_Ctor_Finishes();
        Console.WriteLine("Finished tests");
    }
}