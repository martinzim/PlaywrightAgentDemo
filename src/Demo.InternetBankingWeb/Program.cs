using Demo.InternetBankingWeb.Components;
using Demo.InternetBankingWeb.Banking;

namespace Demo.InternetBankingWeb;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();
        builder.Services.AddRazorPages();
        builder.Services.AddScoped<BankingPortalState>();
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorPages();
        app.MapGet("/api/demo/profile", () => Results.Ok(new
        {
            application = "Demo Internet Banking",
            demoCredentials = "demo.klient / Demo123!",
            demoAuthorizationCode = "246810"
        }));
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        app.MapDefaultEndpoints();

        app.Run();
    }
}
