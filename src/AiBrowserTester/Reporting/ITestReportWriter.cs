namespace AiBrowserTester.Reporting;
public interface ITestReportWriter
{
    Task WriteAsync(TestRunRequest request, ScenarioPlan plan, TestRunResult result, CancellationToken cancellationToken);
}