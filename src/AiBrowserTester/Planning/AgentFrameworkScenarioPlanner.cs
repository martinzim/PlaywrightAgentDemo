namespace AiBrowserTester.Planning;
public sealed class AgentFrameworkScenarioPlanner(IAiTestPlannerProvider provider, ScenarioPlanValidator validator) : ITestScenarioPlanner
{
    public async Task<ScenarioPlan> PlanAsync(TestRunRequest request, CancellationToken cancellationToken)
    {
        var plan = await provider.CreatePlanAsync(request, cancellationToken);
        validator.Validate(plan);
        return plan;
    }
}