using Annotations;
using System;
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable NotAccessedField.Local
// ReSharper disable LocalizableElement
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedParameter.Local
#pragma warning disable CS0067 // Event is never used
#pragma warning disable CS8618 

namespace DummyApp.Src;

// An external class representing a service that "observes" objects
public class ExternalRegistry
{
    public static void Register(object instance) 
    {
        // In a real scenario, this might store the instance in a global list
        // accessible by other threads.
    }

    public ExternalRegistry(object instance)
    {
        // Storing the reference during construction is also a leak.
    }
}

public static class GlobalPublisher
{
    public static event Action<string> DataReady;
}

public class SafePublication
{
    private static LeakyConstructor _globalInstance;
    
    // [ThreadSafe]
    private class LeakyConstructor
    {
        public readonly int Data;
        public LeakyConstructor(int value)
        {
            _globalInstance = this; // Scenario 1: Static Assignment
            Data = value;
        }
    }

    // [ThreadSafe]
    private class ExternalCallLeakyConstructor
    {
#pragma warning disable PublicFieldExposed
        public readonly int Data;
#pragma warning restore PublicFieldExposed

        public ExternalCallLeakyConstructor(int value)
        {
            // VIOLATION (Scenario 2): Passing 'this' to an external static method.
            // The analyzer identifies this as 'an external call' because 
            // ExternalRegistry is a different type than ExternalCallLeakyConstructor.
            ExternalRegistry.Register(this);

            // VIOLATION (Scenario 2): Passing 'this' to an external constructor.
            // The analyzer identifies this as 'an external call' because the
            // method being called is a Constructor (MethodKind.Constructor).
            var registry = new ExternalRegistry(this);

            Data = value;
        }
    }

    // [ThreadSafe]
    private class EventLeakyConstructor
    {
        public readonly int Data;
        public EventLeakyConstructor(int value)
        {
            GlobalPublisher.DataReady += this.OnDataReady; // Scenario 3: Event Registration
            Data = value;
        }

        private void OnDataReady(string message) => Console.WriteLine(Data);
    }
}