using System.Text;
using System.Text.Json;
namespace AiBrowserTester.Reporting;
public sealed class MarkdownAndJsonReportWriter : ITestReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public async Task WriteAsync(TestRunRequest request, ScenarioPlan plan, TestRunResult result, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(result.OutputDirectory);
        await File.WriteAllTextAsync(Path.Combine(result.OutputDirectory, "report.json"), JsonSerializer.Serialize(new { request.TargetUrl, request.Prompt, plan.Summary, result.Success, result.FailureReason, Steps = result.StepResults }, JsonOptions), cancellationToken);
        var markdown = new StringBuilder();
        markdown.AppendLine("# AI Browser Test Report");
        markdown.AppendLine();
        markdown.AppendLine($"- Target: `{request.TargetUrl}`");
        markdown.AppendLine($"- Success: `{result.Success}`");
        markdown.AppendLine($"- Summary: {plan.Summary}");
        if (!string.IsNullOrWhiteSpace(result.FailureReason)) markdown.AppendLine($"- Failure reason: {result.FailureReason}");
        markdown.AppendLine();
        markdown.AppendLine("## Steps");
        foreach (var step in result.StepResults) markdown.AppendLine($"- {step.Index}. `{step.Action}` - {step.Description} - Success: `{step.Success}`");
        await File.WriteAllTextAsync(Path.Combine(result.OutputDirectory, "report.md"), markdown.ToString(), cancellationToken);
    }
}