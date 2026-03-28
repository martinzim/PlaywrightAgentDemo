namespace AiBrowserTester.Execution;
public interface IBrowserExecutor
{
    Task<TestRunResult> ExecuteAsync(TestRunRequest request, ScenarioPlan plan, CancellationToken cancellationToken);
}