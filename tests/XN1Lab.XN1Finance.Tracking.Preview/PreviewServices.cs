using XN1Lab.Platform.Branding.Abstractions;
using XN1Lab.Platform.Branding.Models;
using XN1Lab.Platform.Core.Abstractions;
using XN1Lab.Platform.Core.Models;
using XN1Lab.Platform.Identity.Abstractions;
using XN1Lab.Platform.Identity.Models;

namespace XN1Lab.XN1Finance.Tracking.Preview;

internal sealed class PreviewAccountAccessor : ICurrentAccountAccessor
{
    public CurrentAccount Current { get; } = new(new XN1Account
    {
        Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        Username = "interface-preview",
        Email = "preview@example.invalid",
        Name = "Arayüz testi",
        Culture = "tr-TR"
    }, [], []);
    public bool IsAuthenticated => true;
}

internal sealed class PreviewPermissions : IAccountPermissionResolver
{
    public bool HasPermission(CurrentAccount? account, string permission) => account is not null;
    public bool HasAnyPermission(CurrentAccount? account, IEnumerable<string> permissions) => account is not null;
    public bool HasAllPermissions(CurrentAccount? account, IEnumerable<string> permissions) => account is not null;
}

internal sealed class PreviewApplicationContext : IApplicationContextAccessor
{
    public ApplicationContext Current { get; } = new(Guid.Empty, "tracking-ui-preview", "XN1Finance — arayüz testi");
}

internal sealed class PreviewBrandingResolver : IAccountBrandingResolver
{
    public ValueTask<AccountBrandingProfile> ResolveAsync(CurrentAccount? currentAccount,
        CancellationToken cancellationToken = default) => ValueTask.FromResult(new AccountBrandingProfile(
        "tracking-ui-preview", "XN1Finance — arayüz testi",
        new BrandTheme("#14532d", "#334155", "#166534", "#15803d", "#b45309", "#b91c1c",
            "#f6f7f2", "#ffffff", "#111827", "#64748b", "Lato, Arial, sans-serif"),
        new BrandAssetSet(), new BrandNavigationOptions("/finance/portfolio-management/tracking"), []));
}
