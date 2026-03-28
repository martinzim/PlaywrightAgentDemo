namespace AiBrowserTester;
public sealed class ScenarioPlan
{
    public string Summary { get; set; } = string.Empty;
    public IReadOnlyList<TestStep> Steps { get; set; } = [];
}