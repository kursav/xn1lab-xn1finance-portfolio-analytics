using XN1Lab.Platform.UI.Navigation;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Shell;

public sealed class PortfolioAnalyticsModuleRegistry : IPlatformModuleCatalog
{
    private readonly IReadOnlyList<PlatformModuleManifest> _modules =
    [
        new PlatformModuleManifest(
            "xn1finance.portfolio-management",
            "Portfolio Management",
            "/finance/portfolio-management",
            "Finance",
            [PortfolioAnalyticsPermissions.Read],
            "briefcase",
            10)
    ];

    public IReadOnlyList<PlatformModuleManifest> GetModules() => _modules;
}
