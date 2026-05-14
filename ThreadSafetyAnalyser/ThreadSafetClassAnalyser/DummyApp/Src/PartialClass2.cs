
namespace DummyApp.Src;

public static partial class InternalLogger
{
    public static void CallOtherPartialClass()
    {
        Console.WriteLine("Calling other partial class");
        DoWork();
        Console.WriteLine("Done doing work");
    }
}