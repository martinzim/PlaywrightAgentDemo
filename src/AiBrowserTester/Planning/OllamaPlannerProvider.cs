using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AiBrowserTester.Configuration;
using Microsoft.Extensions.Options;

namespace AiBrowserTester.Planning;

public sealed class OllamaPlannerProvider(IHttpClientFactory httpClientFactory, IOptions<AiOptions> options) : IAiTestPlannerProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    public async Task<ScenarioPlan> CreatePlanAsync(TestRunRequest request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var payload = new OllamaChatRequest(settings.Model, false,
        [
            new("system", settings.SystemPrompt),
            new("user", BuildPrompt(request))
        ]);

        Exception? lastException = null;

        foreach (var endpoint in GetEndpointCandidates(settings.Endpoint))
        {
            try
            {
                var client = httpClientFactory.CreateClient(nameof(OllamaPlannerProvider));
                client.BaseAddress = new Uri(endpoint.TrimEnd('/'));
                var response = await client.PostAsJsonAsync("/api/chat", payload, cancellationToken);
                response.EnsureSuccessStatusCode();
                var chatResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(JsonOptions, cancellationToken)
                    ?? throw new InvalidOperationException("Ollama returned an empty response.");
                var content = chatResponse.Message?.Content
                    ?? throw new InvalidOperationException("Ollama response did not contain a message.");
                var json = JsonExtraction.Extract(content);
                return JsonSerializer.Deserialize<ScenarioPlan>(json, JsonOptions)
                    ?? throw new InvalidOperationException("Ollama response JSON could not be deserialized.");
            }
            catch (HttpRequestException ex) when (IsConnectionFailure(ex))
            {
                lastException = ex;
            }
        }

        if (lastException is not null)
        {
            throw new InvalidOperationException(
                $"Unable to reach Ollama from the current environment. Tried: {string.Join(", ", GetEndpointCandidates(settings.Endpoint))}. " +
                "If the tester runs in a container, make sure Ollama is reachable from containers and is not bound only to 127.0.0.1 on the host. " +
                "Typical fixes are setting Ollama to listen on 0.0.0.0:11434 or using the correct host alias for your runtime.",
                lastException);
        }

        throw new InvalidOperationException("Ollama request failed for an unknown reason.");
    }

    private static string BuildPrompt(TestRunRequest request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Create a browser test plan for the following target.");
        builder.AppendLine($"BaseUrl: {request.TargetUrl}");
        builder.AppendLine("Allowed actions: navigate, click, fill, select, press, assert, screenshot.");
        builder.AppendLine("Allowed assertion types: text, title, url, visibility.");
        builder.AppendLine("Prefer stable selectors using data-testid when possible.");
        builder.AppendLine("Do not invent selectors or test ids. Reuse only selectors that are likely to exist on the current page.");
        builder.AppendLine("For the local demo site, prefer these selectors when relevant:");
        builder.AppendLine("- homepage hero title: [data-testid='hero-title']");
        builder.AppendLine("- homepage CTA: [data-testid='hero-cta']");
        builder.AppendLine("- homepage secondary CTA: [data-testid='hero-secondary']");
        builder.AppendLine("- contact form: [data-testid='contact-form']");
        builder.AppendLine("- contact name input: [data-testid='contact-name']");
        builder.AppendLine("- contact email input: [data-testid='contact-email']");
        builder.AppendLine("- contact company input: [data-testid='contact-company']");
        builder.AppendLine("- contact topic select: [data-testid='contact-topic']");
        builder.AppendLine("- contact topic options: Factory reporting, Field service, Support, Executive dashboard");
        builder.AppendLine("- contact message textarea: [data-testid='contact-message']");
        builder.AppendLine("- contact submit button: [data-testid='contact-submit']");
        builder.AppendLine("- contact success: [data-testid='contact-success']");
        builder.AppendLine("- login form: [data-testid='login-form']");
        builder.AppendLine("- login email input: [data-testid='login-email']");
        builder.AppendLine("- login password input: [data-testid='login-password']");
        builder.AppendLine("- login submit button: [data-testid='login-submit']");
        builder.AppendLine("- login error: [data-testid='login-error']");
        builder.AppendLine("- release modal button: [data-testid='release-modal-button']");
        builder.AppendLine("- release modal: [data-testid='release-modal']");
        builder.AppendLine("Return only JSON matching this schema:");
        builder.AppendLine("{ \"summary\": \"short text\", \"steps\": [{ \"action\": \"navigate|click|fill|select|press|assert|screenshot\", \"description\": \"short text\", \"selector\": \"optional CSS selector\", \"value\": \"optional value\", \"url\": \"optional URL\", \"assertion\": { \"type\": \"text|title|url|visibility\", \"selector\": \"optional CSS selector\", \"expected\": \"optional expected value\" } }] }");
        builder.AppendLine("User request:");
        builder.AppendLine(request.Prompt);
        return builder.ToString();
    }

    private static IReadOnlyList<string> GetEndpointCandidates(string configuredEndpoint)
    {
        var normalized = configuredEndpoint.TrimEnd('/');
        var endpoints = new List<string> { normalized };

        if (normalized.Contains("host.containers.internal", StringComparison.OrdinalIgnoreCase))
        {
            endpoints.Add(normalized.Replace("host.containers.internal", "host.docker.internal", StringComparison.OrdinalIgnoreCase));
        }
        else if (normalized.Contains("host.docker.internal", StringComparison.OrdinalIgnoreCase))
        {
            endpoints.Add(normalized.Replace("host.docker.internal", "host.containers.internal", StringComparison.OrdinalIgnoreCase));
        }

        return endpoints
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsConnectionFailure(HttpRequestException exception) =>
        exception.InnerException is System.Net.Sockets.SocketException;

    private sealed record OllamaChatRequest(string Model, bool Stream, IReadOnlyList<OllamaMessage> Messages);
    private sealed record OllamaChatResponse(OllamaMessage? Message);
    private sealed record OllamaMessage(string Role, string Content);
}
