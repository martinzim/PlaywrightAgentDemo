namespace AiBrowserTester;
public sealed class AssertionSpec
{
    public string Type { get; set; } = string.Empty;
    public string? Selector { get; set; }
    public string? Expected { get; set; }
}