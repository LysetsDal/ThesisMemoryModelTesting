using Annotations;

namespace DummyApp.Model;

[ThreadSafe]
public class Turnstile
{
    private int Count { get; set; } = 0;

    private int otherWork = 1;


    public int GetOtherWork()
    {
        return otherWork;
    }    
    
    public void SetOtherWork(int _otherWork)
    {
        otherWork = _otherWork;
    }

    // This method represents a turnstile spinning 1,000 times
    private void Entrance(int increments)
    {
        for (var i = 0; i < increments; i++)
        {
            // The Race Condition happens here
            Count++; 
        }
    }

    public void Run()
    {
        // ===== Turnstile Example ===== 
        var turnstile = new Turnstile();
        // int incrementsPerThread = 10_000;

        var t1 = new Thread(() =>
        {
            // turnstile.Entrance(incrementsPerThread);
            var tmp = GetOtherWork();
        });
        
        var t2 = new Thread(() =>
        {
            // turnstile.Entrance(incrementsPerThread);
            SetOtherWork(42);
        });

        t1.Start(); t2.Start();
        t1.Join(); t2.Join();
    }
}