var builder = DistributedApplication.CreateBuilder(args);

var profile = builder.Configuration["Demo:Profile"] ?? Environment.GetEnvironmentVariable("DEMO_PROFILE") ?? "local-windows";
var aiEndpoint = builder.AddParameter(
    "ai-endpoint",
    builder.Configuration["AI:Endpoint"] ?? Environment.GetEnvironmentVariable("AI__Endpoint") ?? "http://localhost:11434")
    .WithDescription("Ollama endpoint URL used by the AI test planner.")
    .WithCustomInput(parameter => new()
    {
        Name = parameter.Name,
        InputType = InputType.Text,
        Label = "Ollama URL",
        Description = "Base URL of the Ollama API endpoint.",
        Value = builder.Configuration["AI:Endpoint"] ?? Environment.GetEnvironmentVariable("AI__Endpoint") ?? "http://localhost:11434",
        Placeholder = "http://localhost:11434"
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

var web = builder.AddProject<Projects.DemoWeb>("demo-web")
    .WithExternalHttpEndpoints();

if (string.Equals(profile, "local-containerized-runner", StringComparison.OrdinalIgnoreCase))
{
    builder.AddDockerfile("ai-browser-tester-container", "..\\..", "src\\AiBrowserTester\\Dockerfile")
        .WithReference(web)
        .WithEnvironment("Target__BaseUrl", web.GetEndpoint("https"))
        .WithEnvironment("AI__Endpoint", aiEndpoint)
        .WithEnvironment("AI__Model", aiModel)
        .WithEnvironment("Browser__Name", "chromium")
        .WithEnvironment("Browser__Headed", "false")
        .WithEnvironment("Browser__StepDelayMilliseconds", stepDelayMilliseconds)
        .WithEnvironment("Test__PromptFile", selectedPrompt);
}
else
{
    var browser = string.Equals(profile, "ci-agent", StringComparison.OrdinalIgnoreCase) ? "chromium" : "msedge";
    var headed = string.Equals(profile, "local-windows", StringComparison.OrdinalIgnoreCase) ? "true" : "false";

    builder.AddProject<Projects.AiBrowserTester>("ai-browser-tester")
        .WithReference(web)
        .WithEnvironment("Target__BaseUrl", web.GetEndpoint("http"))
        .WithEnvironment("AI__Endpoint", aiEndpoint)
        .WithEnvironment("AI__Model", aiModel)
        .WithEnvironment("Browser__Name", browser)
        .WithEnvironment("Browser__Headed", headed)
        .WithEnvironment("Browser__StepDelayMilliseconds", stepDelayMilliseconds)
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
        return ["homepage-smoke.md"];
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

    return availablePromptFiles.FirstOrDefault(file => string.Equals(file, "homepage-smoke.md", StringComparison.OrdinalIgnoreCase))
        ?? availablePromptFiles.FirstOrDefault()
        ?? "homepage-smoke.md";
}

static string GetRepoRoot() =>
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
