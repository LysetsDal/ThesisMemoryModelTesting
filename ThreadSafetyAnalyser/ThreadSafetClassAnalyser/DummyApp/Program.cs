using DummyApp.Src;

namespace DummyApp;

internal static class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var visibility = new Visibility();

        visibility.GetCount();
        visibility.GetCountLocked();

        var cs = new CorrectlySynchronized();
        cs.TwoLockedThreads_SameLockSymbols();
        cs.TwoLockedThreads_DifferentLockSymbols();
    }
}
