namespace AiBrowserTester.Planning;
public sealed class ScenarioPlanValidator
{
    private static readonly HashSet<string> AllowedActions = new(StringComparer.OrdinalIgnoreCase) { "navigate", "click", "fill", "select", "press", "assert", "screenshot" };
    private static readonly HashSet<string> AllowedAssertions = new(StringComparer.OrdinalIgnoreCase) { "text", "title", "url", "visibility" };
    public void Validate(ScenarioPlan plan)
    {
        if (plan.Steps.Count == 0) throw new InvalidOperationException("The AI provider returned an empty test plan.");
        foreach (var step in plan.Steps)
        {
            if (!AllowedActions.Contains(step.Action)) throw new InvalidOperationException($"Unsupported AI action '{step.Action}'.");
            if (string.Equals(step.Action, "assert", StringComparison.OrdinalIgnoreCase) && (step.Assertion is null || !AllowedAssertions.Contains(step.Assertion.Type)))
            {
                throw new InvalidOperationException("Assertions must use one of: text, title, url, visibility.");
            }
        }
    }
}