namespace AiBrowserTester;
public sealed class BrowserOptions
{
    public string Name { get; set; } = "msedge";
    public string? ChannelOrPath { get; set; }
    public bool Headed { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 60;
    public int StepDelayMilliseconds { get; set; } = 0;
    public int Width { get; set; } = 1440;
    public int Height { get; set; } = 900;
    public bool StartMaximized { get; set; }
}
