using MemoryModelTests.Readonly;

namespace MemoryModelTests;

internal class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Starting tests");
        var readonlyImmutable = new ReadonlyImmutableTest();
        readonlyImmutable.Test_Readonly_Can_Be_Default_Value_After_Ctor_Finishes();
        Console.WriteLine("Finished tests");
    }
}