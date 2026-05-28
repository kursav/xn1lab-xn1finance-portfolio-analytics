using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using XN1Lab.Platform.Identity.Keycloak.Abstractions;
using XN1Lab.Platform.SaaSHost.Hosting;
using XN1Lab.Platform.UI.Navigation;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.PortfolioAnalytics.Services;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Infrastructure.BrowserStorage;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Shell;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

EnsureRequiredConfiguration(builder.Configuration);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddXN1SaaSHost(builder.Configuration, options =>
{
    options.LocalizationResourceType = typeof(App);
    options.ConfigureKeycloakServices = services =>
    {
        services.AddScoped<IKeycloakSessionStore, BrowserKeycloakSessionStore>();
        services.AddScoped<IKeycloakBrowserNavigator, NavigationManagerKeycloakBrowserNavigator>();
    };
});

builder.Services.AddScoped<IPortfolioAnalyticsService, ApiPortfolioAnalyticsService>();
builder.Services.AddScoped<PortfolioAnalyticsPermissionState>();
builder.Services.AddSingleton<PortfolioAnalyticsModuleRegistry>();
builder.Services.AddSingleton<IPlatformModuleCatalog>(sp => sp.GetRequiredService<PortfolioAnalyticsModuleRegistry>());

await builder.Build().RunAsync();

static void EnsureRequiredConfiguration(IConfiguration configuration)
{
    var requiredKeys = new Dictionary<string, string>
    {
        ["Authentication:Provider"] = "Authentication provider",
        ["Application:AppName"] = "Application name",
        ["Application:AppKey"] = "Application key"
    };

    var missingKeys = requiredKeys
        .Where(pair => string.IsNullOrWhiteSpace(configuration[pair.Key]))
        .Select(static pair => $"{pair.Value} (`{pair.Key}`)")
        .ToList();

    if (missingKeys.Count == 0)
    {
        return;
    }

    throw new InvalidOperationException(
        "Required client configuration was not loaded. Missing: " +
        string.Join(", ", missingKeys) +
        ". In Blazor WebAssembly these values should be available from wwwroot/appsettings.json and wwwroot/appsettings.{ENVIRONMENT}.json.");
}
