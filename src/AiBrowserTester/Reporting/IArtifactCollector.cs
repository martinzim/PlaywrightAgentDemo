using Microsoft.Playwright;
namespace AiBrowserTester.Reporting;
public interface IArtifactCollector
{
    Task CaptureAsync(IPage page, string outputDirectory, string prefix, CancellationToken cancellationToken);
}