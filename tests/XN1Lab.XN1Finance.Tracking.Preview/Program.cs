using System.Net;
using Microsoft.Extensions.FileProviders;
using XN1Lab.Platform.Branding.Abstractions;
using XN1Lab.Platform.Branding.Services;
using XN1Lab.Platform.Core.Abstractions;
using XN1Lab.Platform.Identity.Abstractions;
using XN1Lab.Platform.Identity.Services;
using XN1Lab.Platform.UI.Hosting;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.PortfolioAnalytics.Services;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Services;
using XN1Lab.XN1Finance.Tracking.Preview;

var builder = WebApplication.CreateBuilder(args);
// This UI fixture host has no API client, login provider, credentials or broker adapters.
// Explicit loopback binding prevents an environment variable from exposing test permissions.
builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 7547));
builder.WebHost.UseStaticWebAssets();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddPlatformUi();
builder.Services.AddHttpClient<LiveBinanceTrackingService>(client =>
{
    client.BaseAddress = new Uri("https://data-api.binance.vision/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("XN1Finance-Tracking-Preview/1.0");
});
builder.Services.AddScoped<IFinanceTrackingService>(services =>
    services.GetRequiredService<LiveBinanceTrackingService>());
builder.Services.AddScoped<PortfolioAnalyticsPermissionState>();
builder.Services.AddScoped<ICurrentAccountAccessor, PreviewAccountAccessor>();
builder.Services.AddScoped<IAccountPermissionResolver, PreviewPermissions>();
builder.Services.AddScoped<IAccountSessionService, AccountSessionService>();
builder.Services.AddScoped<IApplicationContextAccessor, PreviewApplicationContext>();
builder.Services.AddScoped<IAccountBrandingResolver, PreviewBrandingResolver>();
builder.Services.AddScoped<BrandingStateService>();

var app = builder.Build();
var vendorPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath,
    "../../src/XN1Lab.XN1Finance.PortfolioAnalytics.Web/wwwroot/vendor"));
if (Directory.Exists(vendorPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(vendorPath),
        RequestPath = "/preview-vendor"
    });
}
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<PreviewApp>().AddInteractiveServerRenderMode();
app.Run();
