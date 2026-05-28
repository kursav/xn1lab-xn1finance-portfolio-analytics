using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using XN1Lab.Platform.Identity.Keycloak.Abstractions;
using XN1Lab.Platform.Identity.Keycloak.Configuration;
using XN1Lab.Platform.Identity.Models;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Infrastructure.BrowserStorage;

public sealed class BrowserKeycloakSessionStore(
    IJSRuntime jsRuntime,
    IOptions<KeycloakOptions> options) : IKeycloakSessionStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<AccountSession?> GetSessionAsync(CancellationToken cancellationToken = default)
    {
        var json = await GetItemAsync(options.Value.SessionStorageKey);
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<AccountSession>(json, SerializerOptions);
    }

    public ValueTask SetSessionAsync(AccountSession session, CancellationToken cancellationToken = default) =>
        SetItemAsync(options.Value.SessionStorageKey, JsonSerializer.Serialize(session, SerializerOptions));

    public ValueTask ClearSessionAsync(CancellationToken cancellationToken = default) =>
        RemoveItemAsync(options.Value.SessionStorageKey);

    public ValueTask<string?> GetCodeVerifierAsync(CancellationToken cancellationToken = default) =>
        GetItemAsync(options.Value.PkceStorageKey);

    public ValueTask SetCodeVerifierAsync(string verifier, CancellationToken cancellationToken = default) =>
        SetItemAsync(options.Value.PkceStorageKey, verifier);

    public ValueTask ClearCodeVerifierAsync(CancellationToken cancellationToken = default) =>
        RemoveItemAsync(options.Value.PkceStorageKey);

    private ValueTask<string?> GetItemAsync(string key) =>
        jsRuntime.InvokeAsync<string?>("xn1financeAuthStorage.getItem", key);

    private ValueTask SetItemAsync(string key, string value) =>
        jsRuntime.InvokeVoidAsync("xn1financeAuthStorage.setItem", key, value);

    private ValueTask RemoveItemAsync(string key) =>
        jsRuntime.InvokeVoidAsync("xn1financeAuthStorage.removeItem", key);
}

