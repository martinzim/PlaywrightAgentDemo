using Microsoft.AspNetCore.Mvc.RazorPages;
namespace DemoWeb.Pages;
public class StatusModel : PageModel
{
    public DateTimeOffset LastUpdatedUtc { get; private set; }
    public IReadOnlyList<StatusItem> Items { get; private set; } = [];
    public void OnGet()
    {
        LastUpdatedUtc = DateTimeOffset.UtcNow;
        Items =
        [
            new("status-crm", "CRM integration", "Syncing customer notes every 5 minutes.", true),
            new("status-field", "Field dispatch", "Routing changes are active for all Slovak branches.", true),
            new("status-reporting", "Executive reporting", "Dashboard refresh runs with a 10 minute delay.", false)
        ];
    }
    public sealed record StatusItem(string TestId, string Name, string Description, bool IsHealthy)
    {
        public string Label => IsHealthy ? "Healthy" : "Monitoring";
    }
}