using System;
using Annotations;
using DummyApp.Src;
// ReSharper disable UnusedVariable

namespace DummyApp;

// [ThreadSafe]
internal static class Program
{
    private static readonly object Lock = new();
    
    private static void Main()
    {
        // ----- Visibility -----
        var visibility = new Visibility();
        
        // Should flag warning: FieldDoesNotUseLock
        visibility.GetCount();

        // Prop should flag FieldAccessedExternally warning
        var @break = visibility.PublicPropBreakingEncapsulation;
        
        // Field should flag FieldAccessedExternally warning
        var @field = visibility._transactions;

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
            // Should flag FieldAccessedExternally warning
            visibility.PublicPropBreakingEncapsulation = false;
            var tmp = visibility.PrivateReadonlyProp;

            // Should not flag warning
            var tmp2 = visibility.PublicPropWithSynchronization;

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return visibility;
    }
    
}
