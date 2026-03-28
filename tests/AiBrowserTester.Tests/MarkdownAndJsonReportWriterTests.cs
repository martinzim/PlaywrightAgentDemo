using AiBrowserTester;
using AiBrowserTester.Reporting;

namespace AiBrowserTester.Tests;

public sealed class MarkdownAndJsonReportWriterTests
{
    [Fact]
    public async Task Writes_Both_Report_Formats()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "ai-browser-tester-tests", Guid.NewGuid().ToString("N"));
        var writer = new MarkdownAndJsonReportWriter();
        var request = new TestRunRequest { TargetUrl = "https://example.test", Prompt = "Check the home page.", OutputDirectory = outputDirectory };
        var plan = new ScenarioPlan { Summary = "Smoke test", Steps = [new TestStep { Action = "navigate", Description = "Open home page", Url = "https://example.test" }] };
        var result = new TestRunResult { Success = true, OutputDirectory = outputDirectory, StepResults = [new TestStepResult { Index = 1, Action = "navigate", Description = "Open home page", Success = true }] };
        await writer.WriteAsync(request, plan, result, CancellationToken.None);
        Assert.True(File.Exists(Path.Combine(outputDirectory, "report.json")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "report.md")));
    }
}