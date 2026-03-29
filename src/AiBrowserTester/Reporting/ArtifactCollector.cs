using Microsoft.Playwright;
namespace AiBrowserTester.Reporting;
public sealed class ArtifactCollector : IArtifactCollector
{
    public async Task CaptureAsync(IPage page, string outputDirectory, string prefix, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);

        var screenshotPath = Path.Combine(outputDirectory, $"{prefix}.png");
        try
        {
            // FullPage screenshot can time out on slow/complex pages; use a shorter dedicated timeout
            await page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true, Timeout = 15_000 });
        }
        catch (TimeoutException)
        {
            try
            {
                // Fall back to viewport-only screenshot
                await page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = false, Timeout = 10_000 });
            }
            catch
            {
                // Screenshot is best-effort; swallow so the real failure reason is preserved
            }
        }

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, $"{prefix}.html"),
                await page.ContentAsync(),
                cancellationToken);
        }
        catch
        {
            // HTML capture is best-effort
        }
    }
}