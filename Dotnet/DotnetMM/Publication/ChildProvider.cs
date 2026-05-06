namespace MemoryModelTests.Publication;

public class ChildProvider : ParentProvider
{
    public string[] EscapedReference { get; private set; }
    
    protected override void PublishInternalState(string[] states)
    {
        // The "Alien" method captures the reference!
        EscapedReference = states;
    }
}