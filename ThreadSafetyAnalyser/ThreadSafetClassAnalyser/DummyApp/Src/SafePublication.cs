using Annotations;
using System;
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable NotAccessedField.Local
// ReSharper disable LocalizableElement
#pragma warning disable CS0067 // Event is never used
#pragma warning disable CS8618 

namespace DummyApp.Src;

// External event source for the Event Leak case
public static class GlobalPublisher
{
    public static event Action<string> DataReady;
}

public class SafePublication
{
    // Static assignment 
    private static LeakyConstructor _globalInstance;
    
    [ThreadSafe]
    private class LeakyConstructor
    {
        public readonly int Data;

        public LeakyConstructor(int value)
        {
            // VIOLATION (Scenario 1): Publish 'this' via static assignment
            _globalInstance = this; 
            Data = value;
        }
    }

    [ThreadSafe]
    private class EventLeakyConstructor
    {
        public readonly int Data;

        public EventLeakyConstructor(int value)
        {
            // VIOLATION (Scenario 3): Publish 'this' via event registration
            // This triggers your analyzer because:
            // 1. 'this' is a ThisExpressionSyntax
            // 2. Its parent is a MemberAccessExpression (this.OnDataReady)
            // 3. Its parent is an AddAssignmentExpression (+=)
            GlobalPublisher.DataReady += this.OnDataReady;

            Data = value;
        }

        private void OnDataReady(string message)
        {
            Console.WriteLine($"{message}: {Data}");
        }
    }
}