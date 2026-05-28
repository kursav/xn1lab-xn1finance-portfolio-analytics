using Microsoft.AspNetCore.Components;
using XN1Lab.Platform.Identity.Keycloak.Abstractions;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Infrastructure.BrowserStorage;

public sealed class NavigationManagerKeycloakBrowserNavigator(NavigationManager navigationManager) : IKeycloakBrowserNavigator
{
    public string BaseUri => navigationManager.BaseUri;

    public ValueTask NavigateToAsync(string uri, bool forceLoad = true)
    {
        navigationManager.NavigateTo(uri, forceLoad);
        return ValueTask.CompletedTask;
    }
}

