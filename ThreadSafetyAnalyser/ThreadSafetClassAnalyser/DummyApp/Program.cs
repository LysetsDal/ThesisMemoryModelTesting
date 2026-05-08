using DummyApp.Src;

namespace DummyApp;

internal static class Program
{
    private static readonly object Lock = new();
    
    private static void Main(string[] args)
    {
        // ----- Visibility -----
        var visibility = new Visibility();
        
        // Should flag warning
        visibility.GetCount();
        
        lock (Lock)
        {
            // Should not flag warning
            visibility.GetCount();
        }
        
        // Should not flag warning
        visibility.GetCountLocked();

        // ----- Correctly Synchronized -----
        var cs = new CorrectlySynchronized();
        
        cs.TwoLockedThreads_SameLockSymbols();
        cs.TwoLockedThreads_DifferentLockSymbols();
    }
}
