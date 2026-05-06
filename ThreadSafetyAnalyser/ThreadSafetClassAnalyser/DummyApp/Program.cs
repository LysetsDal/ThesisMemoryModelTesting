using DummyApp.Model;

namespace DummyApp;

internal static class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var test = new Test();

        test.GetCount();
        test.GetCountLocked();
        
        test.MethodWithDoubleNestedLocks();
    }
}
