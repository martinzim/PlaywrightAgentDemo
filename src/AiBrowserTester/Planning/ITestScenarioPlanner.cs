namespace AiBrowserTester.Planning;
public interface ITestScenarioPlanner
{
    Task<ScenarioPlan> PlanAsync(TestRunRequest request, CancellationToken cancellationToken);
}