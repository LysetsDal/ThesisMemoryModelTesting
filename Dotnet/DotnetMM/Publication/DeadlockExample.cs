using Xunit;

namespace MemoryModelTests.Publication;

// This test for a Deadlock by exposing an internal lock 
public class DeadlockTest
{
    private class SharedResource
    {
        // The internal synchronization object
        private readonly object _lock = new();

        // VIOLATION: Exposing the internal lock object via a public accessor.
        // This is exactly what the LockObjectExposedRule is designed to catch.
        public object SyncRoot => _lock;

        public void InternalMethod()
        {
            // This will block if the main thread has hijacked SyncRoot
            lock (_lock)
            {
                // Critical section
            }
        }
    }

    [Fact]
    public async Task Test_ExposedLock_CausesDeadlock_WhenHijackedExternally()
    {
        var resource = new SharedResource();
        var timeout = TimeSpan.FromSeconds(10);

        // We use a Task to run the deadlock logic so we can monitor it for completion
        var testTask = Task.Run(() =>
        {
            // 1. External Hijacking: The caller acquires the "leaked" lock
            lock (resource.SyncRoot)
            {
                // 2. Start a background thread that needs the same internal lock
                var t1 = new Thread(resource.InternalMethod);
                t1.Start();

                // 3. DEADLOCK TRIGGER: 
                // The main test thread waits for t1 to finish (Join).
                // But t1 is blocked waiting for this thread to release 'SyncRoot'.
                t1.Join(); 
            }
        });

        // ASSERT: The task should NOT complete within the timeout period.
        // If the task completes, it means no deadlock occurred.
        // If it hangs, the 'WhenAny' will return the Delay task first, confirming the deadlock.
        var completedTask = await Task.WhenAny(testTask, Task.Delay(timeout));

        Assert.True(completedTask != testTask, 
            "The test should have deadlocked because the internal lock was hijacked externally.");
    }
}