using System;
using Annotations;
using DummyApp.Src;

namespace DummyApp;

// [ThreadSafe]
internal static class Program
{
    private static readonly object Lock = new();
    
    private static void Main(string[] args)
    {
        // ----- Visibility -----
        var visibility = new Visibility();
        
        // Should flag warning
        visibility.GetCount();

        // Should not flag if readonly 
        var externalAccess = visibility._transactionReadOnly;
        
        
        
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

    public static Visibility Connect()
    {
        var visibility = new Visibility();

        try
        {
            visibility.PublicPropBreakingEncapsulation = false;
            var tmp = visibility.PrivateReadonlyProp;

            var tmp2 = visibility.PublicPropWithSynchronization;

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return visibility;
    }
    
}
