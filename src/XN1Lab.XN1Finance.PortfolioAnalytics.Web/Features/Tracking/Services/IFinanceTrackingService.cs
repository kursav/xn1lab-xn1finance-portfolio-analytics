using XN1Lab.Platform.CoreServices.Contracts.PortfolioAnalytics;
using XN1Lab.Platform.Shared.Pagination;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Services;

/// <summary>Account scope and authentication come from the platform session, never from form input.</summary>
public interface IFinanceTrackingService
{
    Task<TrackingCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IndicatorDefinition>> GetIndicatorsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinanceMarketDataCapabilities>> GetProvidersAsync(CancellationToken cancellationToken = default);
    Task<TrackingPlanValidationResult> ValidateAsync(TrackingPlanDefinition definition, CancellationToken cancellationToken = default);
    Task<PaginatedList<TrackingPlan>> GetPlansAsync(TrackingPlanStatus? status = null, string? search = null,
        int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<TrackingPlan> GetPlanAsync(Guid planId, CancellationToken cancellationToken = default);
    Task<PaginatedList<TrackingPlanVersion>> GetVersionsAsync(Guid planId, int pageNumber = 1,
        int pageSize = 50, CancellationToken cancellationToken = default);
    Task<TrackingPlanVersion> GetVersionAsync(Guid planId, int versionNumber, CancellationToken cancellationToken = default);
    Task<TrackingPlan> CreatePlanAsync(SaveTrackingPlanRequest request, CancellationToken cancellationToken = default);
    Task<TrackingPlan> UpdatePlanAsync(Guid planId, SaveTrackingPlanRequest request, CancellationToken cancellationToken = default);
    Task<TrackingPlan> ChangeStatusAsync(Guid planId, ChangeTrackingPlanStatusRequest request, CancellationToken cancellationToken = default);
    Task<SecurityInstrumentContract> ImportInstrumentAsync(ImportFinanceInstrumentRequest request, CancellationToken cancellationToken = default);
    Task<SecurityInstrumentContract> GetInstrumentAsync(Guid instrumentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinancePreviewResult>> PreviewAsync(Guid planId, CancellationToken cancellationToken = default);
    Task<PaginatedList<FinanceEvaluation>> GetEvaluationsAsync(Guid planId, int pageNumber = 1,
        int pageSize = 50, CancellationToken cancellationToken = default);
    Task<PaginatedList<FinanceSignal>> GetSignalsAsync(Guid planId, int pageNumber = 1,
        int pageSize = 50, CancellationToken cancellationToken = default);
    Task<FinancePaperPortfolio> GetPaperAsync(Guid planId, CancellationToken cancellationToken = default);
    Task<PaginatedList<FinancePaperTrade>> GetPaperTradesAsync(Guid planId, int pageNumber = 1,
        int pageSize = 50, CancellationToken cancellationToken = default);
    Task<PaginatedList<FinanceNotification>> GetNotificationsAsync(Guid? planId = null, bool unreadOnly = false,
        int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task MarkNotificationReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
}
