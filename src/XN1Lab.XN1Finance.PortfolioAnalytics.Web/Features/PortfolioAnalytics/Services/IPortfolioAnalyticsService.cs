using XN1Lab.Platform.CoreServices.Contracts.PortfolioAnalytics;
using XN1Lab.Platform.Shared.Pagination;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.PortfolioAnalytics.Models;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.PortfolioAnalytics.Services;

public interface IPortfolioAnalyticsService
{
    Task<PortfolioAnalyticsDashboardSnapshot> GetDashboardSnapshotAsync(CancellationToken cancellationToken = default);
    Task<PaginatedList<PortfolioContract>> GetPortfoliosAsync(
        string? searchText = null,
        PortfolioStatus? status = null,
        string? baseCurrency = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
    Task<PortfolioContract?> GetPortfolioByIdAsync(
        Guid portfolioId,
        CancellationToken cancellationToken = default);
    Task<PortfolioContract> SavePortfolioAsync(
        PortfolioContract portfolio,
        CancellationToken cancellationToken = default);
    Task DeletePortfolioAsync(
        Guid portfolioId,
        CancellationToken cancellationToken = default);
    Task<PaginatedList<PortfolioHoldingContract>> GetHoldingsAsync(
        Guid portfolioId,
        PortfolioHoldingStatus? status = null,
        PortfolioInstrumentType? instrumentType = null,
        string? symbol = null,
        string? sectorKey = null,
        string? searchText = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
    Task<PortfolioHoldingContract> SaveHoldingAsync(
        Guid portfolioId,
        PortfolioHoldingContract holding,
        CancellationToken cancellationToken = default);
    Task DeleteHoldingAsync(
        Guid portfolioId,
        Guid holdingId,
        CancellationToken cancellationToken = default);
    Task<PortfolioExposureSnapshotContract?> GetPortfolioExposureAsync(
        Guid portfolioId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortfolioEarningsEventContract>> GetPortfolioEarningsAsync(
        Guid portfolioId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default);
    Task<PortfolioScenarioRunResultContract?> RunPortfolioScenarioAsync(
        Guid portfolioId,
        PortfolioScenarioRunRequestContract request,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityInstrumentContract>> LookupInstrumentsAsync(
        string symbol,
        string? exchangeCode = null,
        int take = 20,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityInstrumentContract>> SearchInstrumentsAsync(
        string searchText,
        string? exchangeCode = null,
        int take = 20,
        CancellationToken cancellationToken = default);
    Task<SecurityInstrumentContract?> ResolveExternalInstrumentAsync(
        string symbol,
        string? providerSymbol = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MacroCalendarEventContract>> GetMacroCalendarAsync(
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        MacroSeriesImportance? importance = null,
        string? countryCode = null,
        string? category = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityEarningsEventContract>> GetEarningsCalendarAsync(
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        string? symbol = null,
        string? sectorKey = null,
        CancellationToken cancellationToken = default);
    Task<PortfolioAnalyticsAccountSettingsContract> GetAccountSettingsAsync(CancellationToken cancellationToken = default);
    Task<PortfolioAnalyticsAccountSettingsContract> SaveAccountSettingsAsync(
        PortfolioAnalyticsAccountSettingsContract settings,
        CancellationToken cancellationToken = default);
    Task<PortfolioAnalyticsSystemSettingsContract> GetSystemSettingsAsync(CancellationToken cancellationToken = default);
    Task<PortfolioAnalyticsSystemSettingsContract> SaveSystemSettingsAsync(
        PortfolioAnalyticsSystemSettingsContract settings,
        CancellationToken cancellationToken = default);
}
