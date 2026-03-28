namespace AiBrowserTester;
public sealed class TestRunRequest
{
    public string TargetUrl { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string PromptSource { get; set; } = "inline";
    public BrowserOptions Browser { get; set; } = new();
    public string OutputDirectory { get; set; } = "output/playwright";
}
