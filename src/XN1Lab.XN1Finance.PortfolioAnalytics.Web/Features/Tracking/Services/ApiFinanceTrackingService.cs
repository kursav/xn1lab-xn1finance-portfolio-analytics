using System.Globalization;
using System.Net;
using System.Text.Json;
using XN1Lab.Platform.Api.Abstractions;
using XN1Lab.Platform.Api.Exceptions;
using XN1Lab.Platform.CoreServices.Contracts.PortfolioAnalytics;
using XN1Lab.Platform.Shared.Pagination;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Services;

public sealed class ApiFinanceTrackingService(IPlatformApiClient apiClient) : IFinanceTrackingService
{
    private const string BasePath = "api/xn1finance/tracking-plans";

    public Task<TrackingCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default) =>
        SendRequiredAsync<TrackingCapabilities>(HttpMethod.Get, $"{BasePath}/capabilities", cancellationToken);

    public async Task<IReadOnlyList<IndicatorDefinition>> GetIndicatorsAsync(CancellationToken cancellationToken = default) =>
        await SendRequiredAsync<List<IndicatorDefinition>>(HttpMethod.Get, $"{BasePath}/indicators", cancellationToken);

    public async Task<IReadOnlyList<FinanceMarketDataCapabilities>> GetProvidersAsync(CancellationToken cancellationToken = default) =>
        await SendRequiredAsync<List<FinanceMarketDataCapabilities>>(HttpMethod.Get, $"{BasePath}/providers", cancellationToken);

    public Task<TrackingPlanValidationResult> ValidateAsync(TrackingPlanDefinition definition, CancellationToken cancellationToken = default) =>
        SendRequiredAsync<TrackingPlanValidationResult>(HttpMethod.Post, $"{BasePath}/validate", cancellationToken,
            body: definition ?? throw new ArgumentNullException(nameof(definition)));

    public Task<PaginatedList<TrackingPlan>> GetPlansAsync(TrackingPlanStatus? status = null, string? search = null,
        int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var query = Paging(pageNumber, pageSize);
        query["status"] = status?.ToString();
        query["search"] = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        return SendRequiredAsync<PaginatedList<TrackingPlan>>(HttpMethod.Get, BasePath, cancellationToken, query);
    }

    public Task<TrackingPlan> GetPlanAsync(Guid planId, CancellationToken cancellationToken = default) =>
        SendRequiredAsync<TrackingPlan>(HttpMethod.Get, PlanPath(planId), cancellationToken);

    public Task<PaginatedList<TrackingPlanVersion>> GetVersionsAsync(Guid planId, int pageNumber = 1,
        int pageSize = 50, CancellationToken cancellationToken = default) =>
        SendRequiredAsync<PaginatedList<TrackingPlanVersion>>(HttpMethod.Get, $"{PlanPath(planId)}/versions",
            cancellationToken, Paging(pageNumber, pageSize));

    public Task<TrackingPlanVersion> GetVersionAsync(Guid planId, int versionNumber, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(versionNumber, 1);
        return SendRequiredAsync<TrackingPlanVersion>(HttpMethod.Get,
            $"{PlanPath(planId)}/versions/{versionNumber.ToString(CultureInfo.InvariantCulture)}", cancellationToken);
    }

    public Task<TrackingPlan> CreatePlanAsync(SaveTrackingPlanRequest request, CancellationToken cancellationToken = default) =>
        SendRequiredAsync<TrackingPlan>(HttpMethod.Post, BasePath, cancellationToken,
            body: request ?? throw new ArgumentNullException(nameof(request)));

    public Task<TrackingPlan> UpdatePlanAsync(Guid planId, SaveTrackingPlanRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ExpectedRevision is not > 0)
            throw new ArgumentException("The loaded plan revision is required to update a plan.", nameof(request));
        return SendRequiredAsync<TrackingPlan>(HttpMethod.Put, PlanPath(planId), cancellationToken, body: request);
    }

    public Task<TrackingPlan> ChangeStatusAsync(Guid planId, ChangeTrackingPlanStatusRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.ExpectedRevision, 1);
        return SendRequiredAsync<TrackingPlan>(HttpMethod.Patch, $"{PlanPath(planId)}/status", cancellationToken, body: request);
    }

    public Task<SecurityInstrumentContract> ImportInstrumentAsync(ImportFinanceInstrumentRequest request, CancellationToken cancellationToken = default) =>
        SendRequiredAsync<SecurityInstrumentContract>(HttpMethod.Post, $"{BasePath}/instruments/import", cancellationToken,
            body: request ?? throw new ArgumentNullException(nameof(request)));

    public Task<SecurityInstrumentContract> GetInstrumentAsync(Guid instrumentId, CancellationToken cancellationToken = default)
    {
        RequireId(instrumentId, nameof(instrumentId));
        return SendRequiredAsync<SecurityInstrumentContract>(HttpMethod.Get,
            $"api/xn1finance/portfolio-analytics/instruments/{instrumentId:D}", cancellationToken);
    }

    public async Task<IReadOnlyList<FinancePreviewResult>> PreviewAsync(Guid planId, CancellationToken cancellationToken = default) =>
        await SendRequiredAsync<List<FinancePreviewResult>>(HttpMethod.Post, $"{PlanPath(planId)}/preview", cancellationToken);

    public Task<PaginatedList<FinanceEvaluation>> GetEvaluationsAsync(Guid planId, int pageNumber = 1,
        int pageSize = 50, CancellationToken cancellationToken = default) =>
        SendRequiredAsync<PaginatedList<FinanceEvaluation>>(HttpMethod.Get, $"{PlanPath(planId)}/evaluations",
            cancellationToken, Paging(pageNumber, pageSize));

    public Task<PaginatedList<FinanceSignal>> GetSignalsAsync(Guid planId, int pageNumber = 1,
        int pageSize = 50, CancellationToken cancellationToken = default) =>
        SendRequiredAsync<PaginatedList<FinanceSignal>>(HttpMethod.Get, $"{PlanPath(planId)}/signals",
            cancellationToken, Paging(pageNumber, pageSize));

    public Task<FinancePaperPortfolio> GetPaperAsync(Guid planId, CancellationToken cancellationToken = default) =>
        SendRequiredAsync<FinancePaperPortfolio>(HttpMethod.Get, $"{PlanPath(planId)}/paper", cancellationToken);

    public Task<PaginatedList<FinancePaperTrade>> GetPaperTradesAsync(Guid planId, int pageNumber = 1,
        int pageSize = 50, CancellationToken cancellationToken = default) =>
        SendRequiredAsync<PaginatedList<FinancePaperTrade>>(HttpMethod.Get, $"{PlanPath(planId)}/paper/trades",
            cancellationToken, Paging(pageNumber, pageSize));

    public Task<PaginatedList<FinanceNotification>> GetNotificationsAsync(Guid? planId = null, bool unreadOnly = false,
        int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var query = Paging(pageNumber, pageSize);
        query["planId"] = planId?.ToString("D");
        query["unreadOnly"] = unreadOnly ? "true" : "false";
        return SendRequiredAsync<PaginatedList<FinanceNotification>>(HttpMethod.Get,
            $"{BasePath}/notifications", cancellationToken, query);
    }

    public async Task MarkNotificationReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        RequireId(notificationId, nameof(notificationId));
        var path = $"{BasePath}/notifications/{notificationId:D}/read";
        var response = await apiClient.SendAsync(HttpMethod.Patch, path, cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new PlatformApiException(response.StatusCode, response.Content);
        if (response.StatusCode != HttpStatusCode.NoContent)
            throw new FinanceTrackingApiContractException(path);
    }

    private async Task<T> SendRequiredAsync<T>(HttpMethod method, string path, CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string?>? query = null, object? body = null) where T : class
    {
        var response = await apiClient.SendAsync(method, path, query, body, cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new PlatformApiException(response.StatusCode, response.Content);
        try
        {
            return response.ReadJson<T>() ?? throw new FinanceTrackingApiContractException(path);
        }
        catch (JsonException ex)
        {
            throw new FinanceTrackingApiContractException(path, ex);
        }
    }

    private static Dictionary<string, string?> Paging(int pageNumber, int pageSize)
    {
        if (pageNumber is < 1 or > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        if (pageSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        return new Dictionary<string, string?>
        {
            ["pageNumber"] = pageNumber.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
        };
    }

    private static string PlanPath(Guid planId)
    {
        RequireId(planId, nameof(planId));
        return $"{BasePath}/{planId:D}";
    }

    private static void RequireId(Guid id, string parameterName)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("An existing identifier is required.", parameterName);
    }
}

/// <summary>Distinguishes a broken response from a successful empty collection or a valid draft.</summary>
public sealed class FinanceTrackingApiContractException(string path, Exception? innerException = null)
    : InvalidOperationException($"The finance tracking API returned an invalid response for {path}.", innerException);
