namespace MemoryModelTests.Publication;

public class ParentProvider
{
    private string[] _secretStates = { "Alpha", "Beta", "Gamma" };

    public ParentProvider()
    {
        // UNSAFE: This is an "alien method" call as Goetz describes.
        PublishInternalState(_secretStates);
    }
    
    // UNSAFE: Passing the internal state to a child that implements the virtual method.
    protected virtual void PublishInternalState(string[] states)
    {
    }
    
    // The ref also escapes here, but it's only used as a utility function for the
    // unit test.
    public string[] GetStates() => _secretStates;
}

