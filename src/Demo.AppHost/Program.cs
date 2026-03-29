var builder = DistributedApplication.CreateBuilder(args);

var profile = builder.Configuration["Demo:Profile"] ?? Environment.GetEnvironmentVariable("DEMO_PROFILE") ?? "local-windows";

// Podman on WSL2 uses host.docker.internal (host.containers.internal is not routed correctly)
var isContainerizedRunner = string.Equals(profile, "local-containerized-runner", StringComparison.OrdinalIgnoreCase);
var defaultAiEndpoint = builder.Configuration["AI:Endpoint"]
    ?? Environment.GetEnvironmentVariable("AI__Endpoint")
    ?? (isContainerizedRunner ? "http://host.docker.internal:11434" : "http://localhost:11434");

var aiEndpoint = builder.AddParameter("ai-endpoint", defaultAiEndpoint)
    .WithDescription("Ollama endpoint URL used by the AI test planner.")
    .WithCustomInput(parameter => new()
    {
        Name = parameter.Name,
        InputType = InputType.Text,
        Label = "Ollama URL",
        Description = "Base URL of the Ollama API endpoint.",
        Value = defaultAiEndpoint,
        Placeholder = isContainerizedRunner ? "http://host.docker.internal:11434" : "http://localhost:11434"
    });

var aiModel = builder.AddParameter(
    "ai-model",
    builder.Configuration["AI:Model"] ?? Environment.GetEnvironmentVariable("AI__Model") ?? "llama3.2")
    .WithDescription("Ollama model name used to generate the structured browser test plan.")
    .WithCustomInput(parameter => new()
    {
        Name = parameter.Name,
        InputType = InputType.Text,
        Label = "Ollama model",
        Description = "Model identifier available in your Ollama instance.",
        Value = builder.Configuration["AI:Model"] ?? Environment.GetEnvironmentVariable("AI__Model") ?? "llama3.2",
        Placeholder = "gpt-oss:20b-cloud"
    });

var aiTimeoutSeconds = builder.AddParameter(
    "ai-timeout-seconds",
    builder.Configuration["AI:TimeoutSeconds"] ?? Environment.GetEnvironmentVariable("AI__TimeoutSeconds") ?? "300")
    .WithDescription("Timeout for the AI planning request in seconds.")
    .WithCustomInput(parameter => new()
    {
        Name = parameter.Name,
        InputType = InputType.Number,
        Label = "AI timeout (s)",
        Description = "Maximum number of seconds to wait for Ollama to return the generated test plan.",
        Value = builder.Configuration["AI:TimeoutSeconds"] ?? Environment.GetEnvironmentVariable("AI__TimeoutSeconds") ?? "300",
        Placeholder = "300"
    });

var stepDelayMilliseconds = builder.AddParameter(
    "browser-step-delay-ms",
    builder.Configuration["Browser:StepDelayMilliseconds"] ?? Environment.GetEnvironmentVariable("Browser__StepDelayMilliseconds") ?? "0")
    .WithDescription("Delay applied after each browser step. Use 0 for no slowdown.")
    .WithCustomInput(parameter => new()
    {
        Name = parameter.Name,
        InputType = InputType.Number,
        Label = "Step delay (ms)",
        Description = "Milliseconds to wait after each test step. Set 0 to disable slowing.",
        Value = builder.Configuration["Browser:StepDelayMilliseconds"] ?? Environment.GetEnvironmentVariable("Browser__StepDelayMilliseconds") ?? "0",
        Placeholder = "0"
    });

var browserWidth = builder.AddParameter(
    "browser-width",
    builder.Configuration["Browser:Width"] ?? Environment.GetEnvironmentVariable("Browser__Width") ?? "1440")
    .WithDescription("Browser viewport width used when Start maximized is disabled.")
    .WithCustomInput(parameter => new()
    {
        Name = parameter.Name,
        InputType = InputType.Number,
        Label = "Browser width",
        Description = "Viewport width in pixels when Start maximized is off.",
        Value = builder.Configuration["Browser:Width"] ?? Environment.GetEnvironmentVariable("Browser__Width") ?? "1440",
        Placeholder = "1440"
    });

var browserHeight = builder.AddParameter(
    "browser-height",
    builder.Configuration["Browser:Height"] ?? Environment.GetEnvironmentVariable("Browser__Height") ?? "900")
    .WithDescription("Browser viewport height used when Start maximized is disabled.")
    .WithCustomInput(parameter => new()
    {
        Name = parameter.Name,
        InputType = InputType.Number,
        Label = "Browser height",
        Description = "Viewport height in pixels when Start maximized is off.",
        Value = builder.Configuration["Browser:Height"] ?? Environment.GetEnvironmentVariable("Browser__Height") ?? "900",
        Placeholder = "900"
    });

var browserStartMaximized = builder.AddParameter(
    "browser-start-maximized",
    builder.Configuration["Browser:StartMaximized"] ?? Environment.GetEnvironmentVariable("Browser__StartMaximized") ?? "false")
    .WithDescription("Start the browser window maximized. When enabled, width and height are ignored.")
    .WithCustomInput(parameter => new()
    {
        Name = parameter.Name,
        InputType = InputType.Choice,
        Label = "Start maximized",
        Description = "Uses a maximized browser window instead of a fixed viewport.",
        Value = builder.Configuration["Browser:StartMaximized"] ?? Environment.GetEnvironmentVariable("Browser__StartMaximized") ?? "false",
        Required = true,
        Options =
        [
            new KeyValuePair<string, string>("false", "False"),
            new KeyValuePair<string, string>("true", "True")
        ]
    });

var promptFiles = GetPromptFiles();
var selectedPrompt = builder.AddParameter(
    "test-prompt-file",
    ResolveSelectedPrompt(
        promptFiles,
        builder.Configuration["Test:PromptFile"] ?? Environment.GetEnvironmentVariable("Test__PromptFile")))
    .WithDescription("Select one of the markdown prompts discovered in src/AiBrowserTester/prompts.")
    .WithCustomInput(parameter => new()
    {
        Name = parameter.Name,
        InputType = InputType.Choice,
        Label = "Prompt",
        Description = "All .md prompts are loaded automatically from src/AiBrowserTester/prompts.",
        Value = ResolveSelectedPrompt(
            promptFiles,
            builder.Configuration["Test:PromptFile"] ?? Environment.GetEnvironmentVariable("Test__PromptFile")),
        Required = true,
        Options = promptFiles
            .Select(file => new KeyValuePair<string, string>(file, Path.GetFileNameWithoutExtension(file)))
            .ToArray()
    });

var attachedTargets = GetAttachedTargets();
var defaultTargetSelection = ResolveSelectedTarget(
    attachedTargets,
    builder.Configuration["Target:Selection"] ?? Environment.GetEnvironmentVariable("Target__Selection"));

var targetSelection = builder.AddParameter("target-selection", defaultTargetSelection)
    .WithDescription("Select which attached web application the tester should open, or choose external-url.")
    .WithCustomInput(parameter => new()
    {
        Name = parameter.Name,
        InputType = InputType.Choice,
        Label = "Target web",
        Description = "Choose one of the attached demo web apps or external-url to type a custom destination.",
        Value = defaultTargetSelection,
        Required = true,
        Options = attachedTargets
            .Select(target => new KeyValuePair<string, string>(target.Key, target.Label))
            .Append(new KeyValuePair<string, string>("external-url", "External URL"))
            .ToArray()
    });

var externalTargetUrl = builder.AddParameter(
    "target-external-url",
    builder.Configuration["Target:ExternalUrl"] ?? Environment.GetEnvironmentVariable("Target__ExternalUrl") ?? string.Empty)
    .WithDescription("Optional external URL used when Target web is set to external-url.")
    .WithCustomInput(parameter => new()
    {
        Name = parameter.Name,
        InputType = InputType.Text,
        Label = "External URL",
        Description = "Used only when Target web is set to external-url.",
        Value = builder.Configuration["Target:ExternalUrl"] ?? Environment.GetEnvironmentVariable("Target__ExternalUrl") ?? string.Empty,
        Placeholder = "https://example.com"
    });

var demoWeb = builder.AddProject<Projects.DemoWeb>("demo-web")
    .WithExternalHttpEndpoints();

var internetBankingWeb = builder.AddProject("internet-banking-web", "..\\Demo.InternetBankingWeb\\Demo.InternetBankingWeb.csproj")
    .WithExternalHttpEndpoints();

if (string.Equals(profile, "local-containerized-runner", StringComparison.OrdinalIgnoreCase))
{
    // launchSettings.json binds to localhost only; override so WSL2 containers reach it via host.docker.internal
    demoWeb.WithEnvironment("ASPNETCORE_URLS",
        ReferenceExpression.Create($"http://0.0.0.0:{demoWeb.GetEndpoint("http").Property(EndpointProperty.Port)}"));
    internetBankingWeb.WithEnvironment("ASPNETCORE_URLS",
        ReferenceExpression.Create($"http://0.0.0.0:{internetBankingWeb.GetEndpoint("http").Property(EndpointProperty.Port)}"));

    builder.AddDockerfile("ai-browser-tester-container", "..\\..", "src\\AiBrowserTester\\Dockerfile")
        .WithReference(demoWeb)
        .WithReference(internetBankingWeb)
        .WithEnvironment("Target__Selection", targetSelection)
        .WithEnvironment("Target__ExternalUrl", externalTargetUrl)
        .WithEnvironment("Target__ServiceDemoWebUrl",
            ReferenceExpression.Create($"http://host.docker.internal:{demoWeb.GetEndpoint("http").Property(EndpointProperty.Port)}"))
        .WithEnvironment("Target__ServiceInternetBankingWebUrl",
            ReferenceExpression.Create($"http://host.docker.internal:{internetBankingWeb.GetEndpoint("http").Property(EndpointProperty.Port)}"))
        .WithEnvironment("AI__Endpoint", aiEndpoint)
        .WithEnvironment("AI__Model", aiModel)
        .WithEnvironment("AI__TimeoutSeconds", aiTimeoutSeconds)
        .WithEnvironment("Browser__Name", "chromium")
        .WithEnvironment("Browser__Headed", "false")
        .WithEnvironment("Browser__StepDelayMilliseconds", stepDelayMilliseconds)
        .WithEnvironment("Browser__Width", browserWidth)
        .WithEnvironment("Browser__Height", browserHeight)
        .WithEnvironment("Browser__StartMaximized", browserStartMaximized)
        .WithEnvironment("Test__PromptFile", selectedPrompt);
}
else
{
    var browser = string.Equals(profile, "ci-agent", StringComparison.OrdinalIgnoreCase) ? "chromium" : "msedge";
    var headed = string.Equals(profile, "local-windows", StringComparison.OrdinalIgnoreCase) ? "true" : "false";
    var preferHttps = string.Equals(profile, "local-windows", StringComparison.OrdinalIgnoreCase);
    var demoWebUrl = preferHttps ? demoWeb.GetEndpoint("https") : demoWeb.GetEndpoint("http");
    var internetBankingWebUrl = preferHttps ? internetBankingWeb.GetEndpoint("https") : internetBankingWeb.GetEndpoint("http");

    builder.AddProject<Projects.AiBrowserTester>("ai-browser-tester")
        .WithReference(demoWeb)
        .WithReference(internetBankingWeb)
        .WithEnvironment("Target__Selection", targetSelection)
        .WithEnvironment("Target__ExternalUrl", externalTargetUrl)
        .WithEnvironment("Target__ServiceDemoWebUrl", demoWebUrl)
        .WithEnvironment("Target__ServiceInternetBankingWebUrl", internetBankingWebUrl)
        .WithEnvironment("AI__Endpoint", aiEndpoint)
        .WithEnvironment("AI__Model", aiModel)
        .WithEnvironment("AI__TimeoutSeconds", aiTimeoutSeconds)
        .WithEnvironment("Browser__Name", browser)
        .WithEnvironment("Browser__Headed", headed)
        .WithEnvironment("Browser__StepDelayMilliseconds", stepDelayMilliseconds)
        .WithEnvironment("Browser__Width", browserWidth)
        .WithEnvironment("Browser__Height", browserHeight)
        .WithEnvironment("Browser__StartMaximized", browserStartMaximized)
        .WithEnvironment("Test__PromptFile", selectedPrompt)
        .WithExplicitStart();
}

await builder
    .Build()
    .RunAsync();

static string[] GetPromptFiles()
{
    var promptDirectory = Path.Combine(GetRepoRoot(), "src", "AiBrowserTester", "prompts");
    if (!Directory.Exists(promptDirectory))
    {
        return ["internet-banking-login-dashboard.md"];
    }

    return Directory
        .EnumerateFiles(promptDirectory, "*.md", SearchOption.TopDirectoryOnly)
        .Select(Path.GetFileName)
        .Where(static file => !string.IsNullOrWhiteSpace(file))
        .OrderBy(static file => file, StringComparer.OrdinalIgnoreCase)
        .Cast<string>()
        .ToArray();
}

static string ResolveSelectedPrompt(IReadOnlyList<string> availablePromptFiles, string? configuredValue)
{
    if (!string.IsNullOrWhiteSpace(configuredValue))
    {
        var fileName = Path.GetFileName(configuredValue);
        if (availablePromptFiles.Any(file => string.Equals(file, fileName, StringComparison.OrdinalIgnoreCase)))
        {
            return fileName;
        }
    }

    return availablePromptFiles.FirstOrDefault(file => string.Equals(file, "internet-banking-login-dashboard.md", StringComparison.OrdinalIgnoreCase))
        ?? availablePromptFiles.FirstOrDefault(file => string.Equals(file, "homepage-smoke.md", StringComparison.OrdinalIgnoreCase))
        ?? availablePromptFiles.FirstOrDefault()
        ?? "internet-banking-login-dashboard.md";
}

static string GetRepoRoot() =>
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

static AttachedTarget[] GetAttachedTargets() =>
[
    new("internet-banking-web", "Demo Internet Banking"),
    new("demo-web", "Demo Company Site")
];

static string ResolveSelectedTarget(IReadOnlyList<AttachedTarget> availableTargets, string? configuredValue)
{
    if (!string.IsNullOrWhiteSpace(configuredValue))
    {
        var normalized = configuredValue.Trim();
        if (string.Equals(normalized, "external-url", StringComparison.OrdinalIgnoreCase))
        {
            return "external-url";
        }

        if (availableTargets.Any(target => string.Equals(target.Key, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return normalized;
        }
    }

    return availableTargets.FirstOrDefault(target => string.Equals(target.Key, "internet-banking-web", StringComparison.OrdinalIgnoreCase))?.Key
        ?? availableTargets.FirstOrDefault()?.Key
        ?? "internet-banking-web";
}

internal sealed record AttachedTarget(string Key, string Label);

