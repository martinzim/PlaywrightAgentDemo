using AiBrowserTester.Configuration;
using AiBrowserTester.Execution;
using AiBrowserTester.Planning;
using AiBrowserTester.Reporting;
using Microsoft.Extensions.Options;

namespace AiBrowserTester.Runner;

public sealed class TestRunOrchestrator(IOptions<BrowserOptions> browserOptions, IOptions<ArtifactOptions> artifactOptions, ITestScenarioPlanner planner, IBrowserExecutor executor, ITestReportWriter reportWriter)
{
    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var request = BuildRequest(args);
        if (string.IsNullOrWhiteSpace(request.TargetUrl) || string.IsNullOrWhiteSpace(request.Prompt))
        {
            Console.Error.WriteLine("Usage: --target <url> --prompt <text> [--browser msedge|chromium] [--headed true|false] [--output path]");
            return 2;
        }
        var plan = await planner.PlanAsync(request, cancellationToken);
        var result = await executor.ExecuteAsync(request, plan, cancellationToken);
        await reportWriter.WriteAsync(request, plan, result, cancellationToken);
        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Output: {result.OutputDirectory}");
        if (!string.IsNullOrWhiteSpace(result.FailureReason)) Console.WriteLine(result.FailureReason);
        return result.Success ? 0 : 1;
    }

    internal TestRunRequest BuildRequest(string[] args)
    {
        var defaults = browserOptions.Value;
        var request = new TestRunRequest
        {
            Browser = new BrowserOptions
            {
                Name = defaults.Name,
                ChannelOrPath = defaults.ChannelOrPath,
                Headed = defaults.Headed,
                TimeoutSeconds = defaults.TimeoutSeconds,
                StepDelayMilliseconds = defaults.StepDelayMilliseconds
            },
            OutputDirectory = Path.GetFullPath(artifactOptions.Value.OutputPath)
        };
        for (var i = 0; i < args.Length; i++)
        {
            var argument = args[i];
            var next = i + 1 < args.Length ? args[i + 1] : null;
            switch (argument)
            {
                case "--target": request.TargetUrl = next ?? request.TargetUrl; i++; break;
                case "--prompt":
                    request.Prompt = next ?? request.Prompt;
                    request.PromptSource = "cli-text";
                    i++;
                    break;
                case "--prompt-file":
                    var cliPromptPath = ResolvePromptPath(next ?? throw new InvalidOperationException("Prompt file is missing."));
                    request.Prompt = File.ReadAllText(cliPromptPath);
                    request.PromptSource = cliPromptPath;
                    i++;
                    break;
                case "--browser": request.Browser.Name = next ?? request.Browser.Name; i++; break;
                case "--headed": request.Browser.Headed = bool.Parse(next ?? "true"); i++; break;
                case "--step-delay-ms": request.Browser.StepDelayMilliseconds = int.Parse(next ?? "0"); i++; break;
                case "--output": request.OutputDirectory = Path.GetFullPath(next ?? request.OutputDirectory); i++; break;
            }
        }
        request.TargetUrl = string.IsNullOrWhiteSpace(request.TargetUrl) ? Environment.GetEnvironmentVariable("Target__BaseUrl") ?? string.Empty : request.TargetUrl;
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            var promptResolution = TryLoadDefaultPrompt();
            request.Prompt = promptResolution.Prompt;
            request.PromptSource = promptResolution.Source;
        }

        return request;
    }

    private static (string Prompt, string Source) TryLoadDefaultPrompt()
    {
        var promptText = Environment.GetEnvironmentVariable("Test__PromptText");
        if (!string.IsNullOrWhiteSpace(promptText))
        {
            return (promptText, "parameter-text");
        }

        var promptFile = Environment.GetEnvironmentVariable("Test__PromptFile");
        if (!string.IsNullOrWhiteSpace(promptFile))
        {
            var resolvedPath = ResolvePromptPath(promptFile);
            if (File.Exists(resolvedPath))
            {
                return (File.ReadAllText(resolvedPath), resolvedPath);
            }
        }

        var preset = Environment.GetEnvironmentVariable("Test__PromptPreset");
        if (!string.IsNullOrWhiteSpace(preset))
        {
            var presetPath = ResolvePromptPath(GetPresetPromptPath(preset));
            if (File.Exists(presetPath))
            {
                return (File.ReadAllText(presetPath), presetPath);
            }
        }

        var defaultPrompt = ResolvePromptPath(GetPresetPromptPath("homepage-smoke"));
        return File.Exists(defaultPrompt)
            ? (File.ReadAllText(defaultPrompt), defaultPrompt)
            : (string.Empty, string.Empty);
    }

    private static string ResolvePromptPath(string path)
    {
        var repoRoot = GetRepoRoot();
        var normalizedPath = path.Trim();

        if (Path.IsPathRooted(normalizedPath))
        {
            return normalizedPath;
        }

        var candidates = GetPathCandidates(normalizedPath, repoRoot)
            .Concat(Path.HasExtension(normalizedPath) ? [] : GetPathCandidates($"{normalizedPath}.md", repoRoot))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    private static IEnumerable<string> GetPathCandidates(string path, string repoRoot)
    {
        return new[]
        {
            Path.GetFullPath(path, Directory.GetCurrentDirectory()),
            Path.GetFullPath(path, AppContext.BaseDirectory),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "prompts", path)),
            Path.GetFullPath(Path.Combine("/app/prompts", path)),
            Path.GetFullPath(Path.Combine(repoRoot, "src", "AiBrowserTester", "prompts", path)),
            Path.GetFullPath(Path.Combine(repoRoot, "src", "AiBrowserTester", path))
        };
    }

    private static string GetPresetPromptPath(string preset)
    {
        var normalized = Path.GetFileNameWithoutExtension(preset.Trim());
        return Path.Combine("prompts", $"{normalized}.md");
    }

    private static string GetRepoRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
