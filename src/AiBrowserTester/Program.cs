using AiBrowserTester;
using AiBrowserTester.Configuration;
using AiBrowserTester.Execution;
using AiBrowserTester.Planning;
using AiBrowserTester.Reporting;
using AiBrowserTester.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Configuration.AddJsonFile("appsettings.json", optional: true).AddEnvironmentVariables();

builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("AI"));
builder.Services.Configure<BrowserOptions>(builder.Configuration.GetSection("Browser"));
builder.Services.Configure<ArtifactOptions>(builder.Configuration.GetSection("Artifacts"));

builder.Services.AddHttpClient(nameof(OllamaPlannerProvider), (serviceProvider, client) =>
{
    var aiOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(Math.Max(aiOptions.TimeoutSeconds, 1));
});

builder.Services.AddSingleton<IAiTestPlannerProvider, OllamaPlannerProvider>();
builder.Services.AddSingleton<ITestScenarioPlanner, AgentFrameworkScenarioPlanner>();
builder.Services.AddSingleton<ScenarioPlanValidator>();
builder.Services.AddSingleton<IBrowserExecutor, PlaywrightBrowserExecutor>();
builder.Services.AddSingleton<IArtifactCollector, ArtifactCollector>();
builder.Services.AddSingleton<ITestReportWriter, MarkdownAndJsonReportWriter>();
builder.Services.AddSingleton<TestRunOrchestrator>();

using var host = builder.Build();

var orchestrator = host.Services.GetRequiredService<TestRunOrchestrator>();

return await orchestrator.RunAsync(args, CancellationToken.None);
