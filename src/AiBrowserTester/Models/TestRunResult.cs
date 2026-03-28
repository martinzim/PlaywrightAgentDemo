namespace AiBrowserTester;
public sealed class TestRunResult
{
    public bool Success { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public IReadOnlyList<TestStepResult> StepResults { get; set; } = [];
    public string OutputDirectory { get; set; } = string.Empty;
}
public sealed class TestStepResult
{
    public int Index { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Details { get; set; }
}