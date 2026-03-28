using AiBrowserTester;
using AiBrowserTester.Planning;

namespace AiBrowserTester.Tests;

public sealed class ScenarioPlanValidatorTests
{
    [Fact]
    public void Accepts_Whitelisted_Actions()
    {
        var validator = new ScenarioPlanValidator();
        var plan = new ScenarioPlan
        {
            Summary = "Safe plan",
            Steps =
            [
                new TestStep { Action = "navigate", Description = "Open home page", Url = "https://example.test" },
                new TestStep { Action = "assert", Description = "Ensure CTA is visible", Assertion = new AssertionSpec { Type = "visibility", Selector = "[data-testid='hero-cta']" } }
            ]
        };
        validator.Validate(plan);
    }

    [Fact]
    public void Rejects_Unsupported_Action()
    {
        var validator = new ScenarioPlanValidator();
        var plan = new ScenarioPlan { Summary = "Unsafe plan", Steps = [new TestStep { Action = "run-code", Description = "Nope" }] };
        var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate(plan));
        Assert.Contains("Unsupported AI action", ex.Message);
    }
}