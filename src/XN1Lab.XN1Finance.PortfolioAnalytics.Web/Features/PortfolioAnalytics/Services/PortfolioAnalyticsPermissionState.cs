using XN1Lab.Platform.Identity.Abstractions;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Shell;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.PortfolioAnalytics.Services;

public sealed class PortfolioAnalyticsPermissionState(
    IAccountPermissionResolver permissionResolver,
    ICurrentAccountAccessor currentAccountAccessor)
{
    public bool CanRead => Has(PortfolioAnalyticsPermissions.Read);
    public bool CanWrite => Has(PortfolioAnalyticsPermissions.Write);
    public bool CanDelete => Has(PortfolioAnalyticsPermissions.Delete);
    public bool CanRunScenario => Has(PortfolioAnalyticsPermissions.ScenarioRun);
    public bool CanReadMacroData => Has(PortfolioAnalyticsPermissions.MacroDataRead);
    public bool CanWriteMacroData => Has(PortfolioAnalyticsPermissions.MacroDataWrite);
    public bool CanReadMarketData => Has(PortfolioAnalyticsPermissions.MarketDataRead);
    public bool CanWriteMarketData => Has(PortfolioAnalyticsPermissions.MarketDataWrite);
    public bool CanReadModels => Has(PortfolioAnalyticsPermissions.ModelsRead);
    public bool CanWriteModels => Has(PortfolioAnalyticsPermissions.ModelsWrite);

    private bool Has(string permission)
    {
        return permissionResolver.HasPermission(currentAccountAccessor.Current, permission);
    }
}
