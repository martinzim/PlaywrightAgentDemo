namespace AiBrowserTester.Planning;
public interface IAiTestPlannerProvider
{
    Task<ScenarioPlan> CreatePlanAsync(TestRunRequest request, CancellationToken cancellationToken);
}