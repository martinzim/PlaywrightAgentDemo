namespace AiBrowserTester;
public sealed class TestStep
{
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Selector { get; set; }
    public string? Value { get; set; }
    public AssertionSpec? Assertion { get; set; }
    public string? Url { get; set; }
}