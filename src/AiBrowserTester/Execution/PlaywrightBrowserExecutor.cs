using AiBrowserTester.Reporting;
using Microsoft.Playwright;

namespace AiBrowserTester.Execution;

public sealed class PlaywrightBrowserExecutor(IArtifactCollector artifactCollector) : IBrowserExecutor
{
    private static readonly Dictionary<string, string> SelectorAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["[data-testid='cta-book-discovery']"] = "[data-testid='hero-cta']",
        ["button[data-testid='cta-book-discovery'], a[data-testid='cta-book-discovery']"] = "[data-testid='hero-cta']",
        ["[data-testid='book-session-cta']"] = "[data-testid='hero-cta']",
        ["button[data-testid='book-session-cta'], a[data-testid='book-session-cta']"] = "[data-testid='hero-cta']",
        ["input[name='email']"] = "[data-testid='login-email']",
        ["input[name=\"email\"]"] = "[data-testid='login-email']",
        ["input[type='email']"] = "[data-testid='login-email']",
        ["input[name='password']"] = "[data-testid='login-password']",
        ["input[name=\"password\"]"] = "[data-testid='login-password']",
        ["input[type='password']"] = "[data-testid='login-password']",
        ["button[type='submit']"] = "[data-testid='login-submit']",
        ["button[type=\"submit\"]"] = "[data-testid='login-submit']",
        ["input[name='name']"] = "[data-testid='contact-name']",
        ["input[name=\"name\"]"] = "[data-testid='contact-name']",
        ["input[name='fullName']"] = "[data-testid='contact-name']",
        ["input[name=\"fullName\"]"] = "[data-testid='contact-name']",
        ["input[name='email']"] = "[data-testid='contact-email']",
        ["input[name=\"email\"]"] = "[data-testid='contact-email']",
        ["input[name='company']"] = "[data-testid='contact-company']",
        ["input[name=\"company\"]"] = "[data-testid='contact-company']",
        ["select[name='topic']"] = "[data-testid='contact-topic']",
        ["select[name=\"topic\"]"] = "[data-testid='contact-topic']",
        ["textarea[name='message']"] = "[data-testid='contact-message']",
        ["textarea[name=\"message\"]"] = "[data-testid='contact-message']"
    };

    public async Task<TestRunResult> ExecuteAsync(TestRunRequest request, ScenarioPlan plan, CancellationToken cancellationToken)
    {
        var stepResults = new List<TestStepResult>();
        Directory.CreateDirectory(request.OutputDirectory);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await LaunchAsync(playwright, request.Browser);
        var page = await browser.NewPageAsync(new() { BaseURL = request.TargetUrl });
        page.SetDefaultTimeout(request.Browser.TimeoutSeconds * 1000);

        try
        {
            for (var index = 0; index < plan.Steps.Count; index++)
            {
                var step = plan.Steps[index];
                await ExecuteStepAsync(page, step, request.OutputDirectory, cancellationToken);
                await ApplyStepDelayAsync(request.Browser.StepDelayMilliseconds, cancellationToken);
                stepResults.Add(new TestStepResult { Index = index + 1, Action = step.Action, Description = step.Description, Success = true });
            }

            await artifactCollector.CaptureAsync(page, request.OutputDirectory, "final", cancellationToken);
            return new TestRunResult { Success = true, Summary = plan.Summary, StepResults = stepResults, OutputDirectory = request.OutputDirectory };
        }
        catch (Exception ex)
        {
            await artifactCollector.CaptureAsync(page, request.OutputDirectory, "failure", cancellationToken);
            stepResults.Add(new TestStepResult { Index = stepResults.Count + 1, Action = "error", Description = "Execution aborted", Success = false, Details = ex.Message });
            return new TestRunResult { Success = false, Summary = plan.Summary, FailureReason = ex.Message, StepResults = stepResults, OutputDirectory = request.OutputDirectory };
        }
    }

    private static async Task<IBrowser> LaunchAsync(IPlaywright playwright, BrowserOptions options)
    {
        var launchOptions = new BrowserTypeLaunchOptions { Headless = !options.Headed };
        if (string.Equals(options.Name, "msedge", StringComparison.OrdinalIgnoreCase))
        {
            launchOptions.Channel = "msedge";
            if (!string.IsNullOrWhiteSpace(options.ChannelOrPath) && File.Exists(options.ChannelOrPath)) launchOptions.ExecutablePath = options.ChannelOrPath;
        }
        else if (!string.IsNullOrWhiteSpace(options.ChannelOrPath) && File.Exists(options.ChannelOrPath))
        {
            launchOptions.ExecutablePath = options.ChannelOrPath;
        }
        return await playwright.Chromium.LaunchAsync(launchOptions);
    }

    private static async Task ExecuteStepAsync(IPage page, TestStep step, string outputDirectory, CancellationToken cancellationToken)
    {
        switch (step.Action.ToLowerInvariant())
        {
            case "navigate":
                await page.GotoAsync(step.Url ?? throw new InvalidOperationException("Navigate step requires url."));
                break;
            case "click":
                await page.Locator(ResolveSelector(RequireSelector(step))).ClickAsync();
                break;
            case "fill":
                await page.Locator(ResolveSelector(RequireSelector(step))).FillAsync(step.Value ?? string.Empty);
                break;
            case "select":
                await SelectOptionAsync(page.Locator(ResolveSelector(RequireSelector(step))), step.Value ?? string.Empty);
                break;
            case "press":
                await page.Locator(ResolveSelector(RequireSelector(step))).PressAsync(step.Value ?? "Enter");
                break;
            case "assert":
                await ExecuteAssertionAsync(page, step.Assertion ?? throw new InvalidOperationException("Assert step is missing assertion."));
                break;
            case "screenshot":
                var fileName = string.IsNullOrWhiteSpace(step.Value) ? "step" : step.Value;
                await page.ScreenshotAsync(new() { Path = Path.Combine(outputDirectory, $"{fileName}.png"), FullPage = true });
                break;
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static Task ApplyStepDelayAsync(int stepDelayMilliseconds, CancellationToken cancellationToken)
    {
        return stepDelayMilliseconds > 0
            ? Task.Delay(stepDelayMilliseconds, cancellationToken)
            : Task.CompletedTask;
    }

    private static async Task ExecuteAssertionAsync(IPage page, AssertionSpec assertion)
    {
        switch (assertion.Type.ToLowerInvariant())
        {
            case "title":
                var title = await page.TitleAsync();
                if (!title.Contains(assertion.Expected ?? string.Empty, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"Expected title to contain '{assertion.Expected}' but was '{title}'.");
                break;
            case "url":
                if (!page.Url.Contains(assertion.Expected ?? string.Empty, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"Expected URL to contain '{assertion.Expected}' but was '{page.Url}'.");
                break;
            case "visibility":
                await page.Locator(ResolveAssertionSelector(assertion)).WaitForAsync(new() { State = WaitForSelectorState.Visible });
                break;
            case "text":
                var locator = page.Locator(ResolveAssertionSelector(assertion));
                var text = await locator.InnerTextAsync();
                if (!text.Contains(assertion.Expected ?? string.Empty, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"Expected text to contain '{assertion.Expected}' but was '{text}'.");
                break;
        }
    }

    private static async Task SelectOptionAsync(ILocator locator, string requestedValue)
    {
        var normalized = requestedValue.Trim();

        var options = await locator.EvaluateAsync<SelectOptionCandidate[]>(
            @"element => Array.from(element.options).map(option => ({
                value: option.value,
                label: option.label,
                text: option.text
            }))");

        var match = options.FirstOrDefault(option =>
            string.Equals(option.Value, normalized, StringComparison.OrdinalIgnoreCase)
            || string.Equals(option.Label, normalized, StringComparison.OrdinalIgnoreCase)
            || string.Equals(option.Text, normalized, StringComparison.OrdinalIgnoreCase)
            || option.Value.Contains(normalized, StringComparison.OrdinalIgnoreCase)
            || option.Label.Contains(normalized, StringComparison.OrdinalIgnoreCase)
            || option.Text.Contains(normalized, StringComparison.OrdinalIgnoreCase)
            || normalized.Contains(option.Value, StringComparison.OrdinalIgnoreCase)
            || normalized.Contains(option.Label, StringComparison.OrdinalIgnoreCase)
            || normalized.Contains(option.Text, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            var availableOptions = string.Join(", ", options.Select(option => $"'{option.Value}'"));
            throw new InvalidOperationException($"No matching select option found for '{requestedValue}'. Available values: {availableOptions}.");
        }

        await locator.SelectOptionAsync(new SelectOptionValue
        {
            Value = match.Value
        });
    }

    private static string RequireSelector(TestStep step) => step.Selector ?? throw new InvalidOperationException($"{step.Action} step requires selector.");

    private static string ResolveAssertionSelector(AssertionSpec assertion)
    {
        var selector = assertion.Selector;

        if (string.IsNullOrWhiteSpace(selector) && LooksLikeSelector(assertion.Expected))
        {
            selector = assertion.Expected;
        }

        return ResolveSelector(selector ?? throw new InvalidOperationException($"{assertion.Type} assertion requires selector."));
    }

    private static string ResolveSelector(string selector)
    {
        var normalized = selector.Trim().Trim('"', '\'');

        if (SelectorAliases.TryGetValue(normalized, out var alias))
        {
            return alias;
        }

        return normalized;
    }

    private static bool LooksLikeSelector(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && (value.Contains("[") || value.Contains(".") || value.Contains("#") || value.Contains("data-testid", StringComparison.OrdinalIgnoreCase));

    private sealed class SelectOptionCandidate
    {
        public string Value { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;
    }
}
