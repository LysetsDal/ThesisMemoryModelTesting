using Xunit;

namespace MemoryModelTests.Publication;

public class EscapingReference
{
    private string[] _secretStates = { "Alpha", "Beta", "Gamma" };

    [Fact]
    public void Test_UnsafePublication_ViaOverriddenMethod()
    {
        // Act: Instantiate the child
        var child = new ChildProvider();

        // Assert: The internal private state of the base class has escaped
        Assert.NotNull(child.EscapedReference);

        // Prove that the original array escapes even thought the parent class,
        // hasn't been initialized.
        var escaped = child.EscapedReference;
        Assert.Equal(_secretStates, escaped);
        
        // Prove any caller can now modify base's private array
        var modified = child.EscapedReference[0] = "COMPROMISED";
        Assert.Equal("COMPROMISED", child.GetStates()[0]);
    }
}
