using Microsoft.Playwright;
namespace AiBrowserTester.Reporting;
public sealed class ArtifactCollector : IArtifactCollector
{
    public async Task CaptureAsync(IPage page, string outputDirectory, string prefix, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        await page.ScreenshotAsync(new() { Path = Path.Combine(outputDirectory, $"{prefix}.png"), FullPage = true });
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, $"{prefix}.html"), await page.ContentAsync(), cancellationToken);
    }
}