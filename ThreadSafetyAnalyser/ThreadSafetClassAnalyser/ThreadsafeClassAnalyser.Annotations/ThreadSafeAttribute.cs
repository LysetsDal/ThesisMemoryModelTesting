using System;

namespace Annotations
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ThreadSafeAttribute : Attribute
    {
        public string LockObjectName { get; }

        // You can add a constructor to pass data, like the name of the lock object
        public ThreadSafeAttribute(string lockObjectName = "_lockObj")
        {
            LockObjectName = lockObjectName;
        }
    }
}