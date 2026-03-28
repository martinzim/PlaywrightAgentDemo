namespace AiBrowserTester.Configuration;
public sealed class AiOptions
{
    public string Provider { get; set; } = "ollama";
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2";
    public string SystemPrompt { get; set; } = "You are a browser test planner. Return only JSON.";
}