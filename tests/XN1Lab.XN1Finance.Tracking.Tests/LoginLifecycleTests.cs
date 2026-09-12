using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using XN1Lab.Platform.Api.Abstractions;
using XN1Lab.Platform.Api;
using XN1Lab.Platform.Identity.Abstractions;
using XN1Lab.Platform.Identity.Keycloak.Abstractions;
using XN1Lab.Platform.Identity.Models;
using XN1Lab.Platform.Identity.Services;
using XN1Lab.Platform.UI.Navigation;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Auth.Pages;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Shell;
using XN1Lab.XN1Finance.Tracking.Tests.Fixtures;

namespace XN1Lab.XN1Finance.Tracking.Tests;

public sealed class LoginLifecycleTests
{
    [Fact]
    public async Task Callback_survives_session_changes_without_reusing_the_authorization_code()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var sessions = new AccountSessionService();
        var auth = new PendingAuthentication();
        var hydrator = new PendingHydrator();
        ctx.Services.AddLogging();
        ctx.Services.AddAuthorizationCore();
        ctx.Services.AddSingleton<AuthenticationStateProvider>(new AnonymousState());
        ctx.Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Authentication:Provider"] = "Keycloak" }).Build());
        ctx.Services.AddSingleton<IAccountSessionService>(sessions);
        ctx.Services.AddSingleton<ICurrentAccountAccessor>(sessions);
        ctx.Services.AddSingleton<IAccountPermissionResolver>(new TrackingTestContext.TestPermissions());
        ctx.Services.AddSingleton<PlatformModuleAccessFilter>();
        ctx.Services.AddSingleton<PortfolioAnalyticsModuleRegistry>();
        ctx.Services.AddSingleton<IKeycloakAuthenticationService>(auth);
        ctx.Services.AddSingleton<ICoreServicesAccountSessionHydrator>(hydrator);
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/auth/callback?code=test-code&state=test-state");
        var app = ctx.Render<App>();
        app.WaitForAssertion(() => Assert.Equal(1, auth.Calls));
        var callback = app.FindComponent<LoginCallbackPage>().Instance;
        var provisional = new AccountSession(new CurrentAccount(new XN1Account { Id = Guid.NewGuid() }, [], []), "test-token", DateTimeOffset.UtcNow);
        await app.InvokeAsync(() => sessions.SetSessionAsync(provisional).AsTask());
        Assert.Same(callback, app.FindComponent<LoginCallbackPage>().Instance);
        Assert.Equal(1, auth.Calls);
        await app.InvokeAsync(() => auth.Completion.SetResult(provisional));
        app.WaitForAssertion(() => Assert.Equal(1, hydrator.Calls));
        var hydrated = provisional with { CurrentAccount = new CurrentAccount(new XN1Account { Id = Guid.NewGuid() }, [], []) };
        await app.InvokeAsync(() => sessions.SetSessionAsync(hydrated).AsTask());
        Assert.Same(callback, app.FindComponent<LoginCallbackPage>().Instance);
        Assert.Equal(1, auth.Calls);
        // An account lookup failure stays visible; it must not become a second code exchange.
        await app.InvokeAsync(() => hydrator.Completion.SetResult(null));
        app.WaitForAssertion(() => Assert.Contains("CoreServices account session could not be loaded.", app.Markup));
        Assert.Equal(1, auth.Calls);
    }

    private sealed class AnonymousState : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class PendingAuthentication : IKeycloakAuthenticationService
    {
        public int Calls { get; private set; }
        public TaskCompletionSource<AccountSession?> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ValueTask<AccountSession?> CompleteSignInAsync(string code, string? state = null, CancellationToken cancellationToken = default)
        {
            Calls++;
            Assert.Equal("test-code", code);
            Assert.Equal("test-state", state);
            return new(Completion.Task);
        }
        public ValueTask<AccountSession?> GetSessionAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult<AccountSession?>(null);
        public ValueTask<AccountSession?> RefreshSessionAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult<AccountSession?>(null);
        public ValueTask SignInAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SignOutAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private sealed class PendingHydrator : ICoreServicesAccountSessionHydrator
    {
        public int Calls { get; private set; }
        public TaskCompletionSource<AccountSession?> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ValueTask<AccountSession?> HydrateAsync(CancellationToken cancellationToken = default) { Calls++; return new(Completion.Task); }
        public ValueTask<AccountSession?> HydrateAsync(Guid? ownerAccountId, CancellationToken cancellationToken = default) => HydrateAsync(cancellationToken);
    }
}
