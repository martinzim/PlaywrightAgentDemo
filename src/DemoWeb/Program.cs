var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapGet("/api/demo/profile", () => Results.Ok(new
{
    company = "Northwind Fabrication",
    slogan = "Industrial software with measurable operations impact.",
    lastUpdatedUtc = DateTimeOffset.UtcNow
}));
app.MapRazorPages()
    .WithStaticAssets();

app.MapDefaultEndpoints();

await app.RunAsync();