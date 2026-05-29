using System.Net;
using XN1Lab.Platform.Api.Abstractions;
using XN1Lab.Platform.Api.Exceptions;
using XN1Lab.Platform.CoreServices.Contracts.PortfolioAnalytics;
using XN1Lab.Platform.Shared.Pagination;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.PortfolioAnalytics.Models;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.PortfolioAnalytics.Services;

public sealed class ApiPortfolioAnalyticsService(IPlatformApiClient apiClient) : IPortfolioAnalyticsService
{
    private const string BasePath = "api/xn1finance/portfolio-analytics";

    private readonly IPlatformApiClient _apiClient = apiClient;

    public async Task<PortfolioAnalyticsDashboardSnapshot> GetDashboardSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var readinessItems = new List<PortfolioAnalyticsReadinessItem>();
        var portfolioCount = 0;
        var macroSeriesCount = 0;
        var baseCurrency = "USD";

        try
        {
            var settings = await GetAccountSettingsAsync(cancellationToken);
            baseCurrency = settings.DefaultBaseCurrency;

            readinessItems.Add(new PortfolioAnalyticsReadinessItem
            {
                Title = "Account settings",
                Status = $"{settings.InvestorProfile} / {settings.DefaultBaseCurrency}",
                Icon = "sliders horizontal"
            });
        }
        catch (PlatformApiException ex) when (ex.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            readinessItems.Add(new PortfolioAnalyticsReadinessItem
            {
                Title = "Account settings",
                Status = "Permission required",
                Icon = "lock"
            });
        }

        try
        {
            var portfolios = await _apiClient.GetAsync<PaginatedList<PortfolioContract>>(
                $"{BasePath}/portfolios",
                new Dictionary<string, string?>
                {
                    ["pageNumber"] = "1",
                    ["pageSize"] = "1"
                },
                cancellationToken: cancellationToken);

            portfolioCount = portfolios?.TotalItems ?? 0;

            readinessItems.Add(new PortfolioAnalyticsReadinessItem
            {
                Title = "Portfolio API",
                Status = "Connected",
                Icon = "server"
            });
        }
        catch (PlatformApiException ex) when (ex.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            readinessItems.Add(new PortfolioAnalyticsReadinessItem
            {
                Title = "Portfolio API",
                Status = "Permission required",
                Icon = "lock"
            });
        }

        try
        {
            var macroSeries = await _apiClient.GetAsync<PaginatedList<MacroSeriesContract>>(
                $"{BasePath}/macro/series",
                new Dictionary<string, string?>
                {
                    ["pageNumber"] = "1",
                    ["pageSize"] = "1"
                },
                cancellationToken: cancellationToken);

            macroSeriesCount = macroSeries?.TotalItems ?? 0;

            readinessItems.Add(new PortfolioAnalyticsReadinessItem
            {
                Title = "Macro catalog",
                Status = $"{macroSeriesCount} series",
                Icon = "chart line"
            });
        }
        catch (PlatformApiException ex) when (ex.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            readinessItems.Add(new PortfolioAnalyticsReadinessItem
            {
                Title = "Macro catalog",
                Status = "Permission required",
                Icon = "lock"
            });
        }

        var snapshot = new PortfolioAnalyticsDashboardSnapshot
        {
            PortfolioCount = portfolioCount,
            HoldingCount = 0,
            EstimatedValue = 0m,
            BaseCurrency = baseCurrency,
            ReadinessItems = readinessItems
        };

        return snapshot;
    }

    public async Task<PaginatedList<PortfolioContract>> GetPortfoliosAsync(
        string? searchText = null,
        PortfolioStatus? status = null,
        string? baseCurrency = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["searchText"] = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim(),
            ["status"] = status?.ToString(),
            ["baseCurrency"] = string.IsNullOrWhiteSpace(baseCurrency) ? null : baseCurrency.Trim(),
            ["pageNumber"] = pageNumber.ToString(),
            ["pageSize"] = pageSize.ToString()
        };

        return await _apiClient.GetAsync<PaginatedList<PortfolioContract>>(
            $"{BasePath}/portfolios",
            query,
            cancellationToken: cancellationToken)
            ?? new PaginatedList<PortfolioContract>();
    }

    public async Task<PortfolioContract?> GetPortfolioByIdAsync(
        Guid portfolioId,
        CancellationToken cancellationToken = default)
    {
        return await _apiClient.GetAsync<PortfolioContract>(
            $"{BasePath}/portfolios/{portfolioId}",
            cancellationToken: cancellationToken);
    }

    public async Task<PortfolioContract> SavePortfolioAsync(
        PortfolioContract portfolio,
        CancellationToken cancellationToken = default)
    {
        if (portfolio.Id == Guid.Empty)
        {
            return await _apiClient.PostAsync<PortfolioContract, PortfolioContract>(
                $"{BasePath}/portfolios",
                portfolio,
                cancellationToken: cancellationToken)
                ?? portfolio;
        }

        var response = await _apiClient.SendAsync(
            HttpMethod.Put,
            $"{BasePath}/portfolios/{portfolio.Id}",
            body: portfolio,
            cancellationToken: cancellationToken);

        return response.ReadJson<PortfolioContract>() ?? portfolio;
    }

    public async Task DeletePortfolioAsync(
        Guid portfolioId,
        CancellationToken cancellationToken = default)
    {
        await _apiClient.SendAsync(
            HttpMethod.Delete,
            $"{BasePath}/portfolios/{portfolioId}",
            cancellationToken: cancellationToken);
    }

    public async Task<PaginatedList<PortfolioHoldingContract>> GetHoldingsAsync(
        Guid portfolioId,
        PortfolioHoldingStatus? status = null,
        PortfolioInstrumentType? instrumentType = null,
        string? symbol = null,
        string? sectorKey = null,
        string? searchText = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["status"] = status?.ToString(),
            ["instrumentType"] = instrumentType?.ToString(),
            ["symbol"] = string.IsNullOrWhiteSpace(symbol) ? null : symbol.Trim(),
            ["sectorKey"] = string.IsNullOrWhiteSpace(sectorKey) ? null : sectorKey.Trim(),
            ["searchText"] = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim(),
            ["pageNumber"] = pageNumber.ToString(),
            ["pageSize"] = pageSize.ToString()
        };

        return await _apiClient.GetAsync<PaginatedList<PortfolioHoldingContract>>(
            $"{BasePath}/portfolios/{portfolioId}/holdings",
            query,
            cancellationToken: cancellationToken)
            ?? new PaginatedList<PortfolioHoldingContract>();
    }

    public async Task<PortfolioHoldingContract> SaveHoldingAsync(
        Guid portfolioId,
        PortfolioHoldingContract holding,
        CancellationToken cancellationToken = default)
    {
        holding.PortfolioId = portfolioId;

        if (holding.Id == Guid.Empty)
        {
            return await _apiClient.PostAsync<PortfolioHoldingContract, PortfolioHoldingContract>(
                $"{BasePath}/portfolios/{portfolioId}/holdings",
                holding,
                cancellationToken: cancellationToken)
                ?? holding;
        }

        var response = await _apiClient.SendAsync(
            HttpMethod.Put,
            $"{BasePath}/portfolios/{portfolioId}/holdings/{holding.Id}",
            body: holding,
            cancellationToken: cancellationToken);

        return response.ReadJson<PortfolioHoldingContract>() ?? holding;
    }

    public async Task DeleteHoldingAsync(
        Guid portfolioId,
        Guid holdingId,
        CancellationToken cancellationToken = default)
    {
        await _apiClient.SendAsync(
            HttpMethod.Delete,
            $"{BasePath}/portfolios/{portfolioId}/holdings/{holdingId}",
            cancellationToken: cancellationToken);
    }

    public async Task<PortfolioExposureSnapshotContract?> GetPortfolioExposureAsync(
        Guid portfolioId,
        CancellationToken cancellationToken = default)
    {
        return await _apiClient.GetAsync<PortfolioExposureSnapshotContract>(
            $"{BasePath}/portfolios/{portfolioId}/exposure",
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<PortfolioEarningsEventContract>> GetPortfolioEarningsAsync(
        Guid portfolioId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["fromUtc"] = fromUtc?.ToUniversalTime().ToString("O"),
            ["toUtc"] = toUtc?.ToUniversalTime().ToString("O")
        };

        return await _apiClient.GetAsync<List<PortfolioEarningsEventContract>>(
            $"{BasePath}/portfolios/{portfolioId}/earnings",
            query,
            cancellationToken: cancellationToken)
            ?? [];
    }

    public async Task<PortfolioScenarioRunResultContract?> RunPortfolioScenarioAsync(
        Guid portfolioId,
        PortfolioScenarioRunRequestContract request,
        CancellationToken cancellationToken = default)
    {
        return await _apiClient.PostAsync<PortfolioScenarioRunRequestContract, PortfolioScenarioRunResultContract>(
            $"{BasePath}/portfolios/{portfolioId}/scenarios/run",
            request,
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityInstrumentContract>> LookupInstrumentsAsync(
        string symbol,
        string? exchangeCode = null,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["symbol"] = string.IsNullOrWhiteSpace(symbol) ? null : symbol.Trim(),
            ["exchangeCode"] = string.IsNullOrWhiteSpace(exchangeCode) ? null : exchangeCode.Trim(),
            ["take"] = take.ToString()
        };

        return await _apiClient.GetAsync<List<SecurityInstrumentContract>>(
            $"{BasePath}/instruments/lookup",
            query,
            cancellationToken: cancellationToken)
            ?? [];
    }

    public async Task<IReadOnlyList<SecurityInstrumentContract>> SearchInstrumentsAsync(
        string searchText,
        string? exchangeCode = null,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["searchText"] = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim(),
            ["take"] = Math.Clamp(take, 1, 100).ToString()
        };

        return await _apiClient.GetAsync<List<SecurityInstrumentContract>>(
            $"{BasePath}/instruments/external-search",
            query,
            cancellationToken: cancellationToken)
            ?? [];
    }

    public async Task<SecurityInstrumentContract?> ResolveExternalInstrumentAsync(
        string symbol,
        string? providerSymbol = null,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["symbol"] = string.IsNullOrWhiteSpace(symbol) ? null : symbol.Trim(),
            ["providerSymbol"] = string.IsNullOrWhiteSpace(providerSymbol) ? null : providerSymbol.Trim()
        };

        try
        {
            return await _apiClient.GetAsync<SecurityInstrumentContract>(
                $"{BasePath}/instruments/external-resolve",
                query,
                cancellationToken: cancellationToken);
        }
        catch (PlatformApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<MacroCalendarEventContract>> GetMacroCalendarAsync(
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        MacroSeriesImportance? importance = null,
        string? countryCode = null,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["fromUtc"] = fromUtc?.ToUniversalTime().ToString("O"),
            ["toUtc"] = toUtc?.ToUniversalTime().ToString("O"),
            ["importance"] = importance?.ToString(),
            ["countryCode"] = string.IsNullOrWhiteSpace(countryCode) ? null : countryCode.Trim(),
            ["category"] = string.IsNullOrWhiteSpace(category) ? null : category.Trim()
        };

        return await _apiClient.GetAsync<List<MacroCalendarEventContract>>(
            $"{BasePath}/macro/calendar",
            query,
            cancellationToken: cancellationToken)
            ?? [];
    }

    public async Task<IReadOnlyList<SecurityEarningsEventContract>> GetEarningsCalendarAsync(
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        string? symbol = null,
        string? sectorKey = null,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["fromUtc"] = fromUtc?.ToUniversalTime().ToString("O"),
            ["toUtc"] = toUtc?.ToUniversalTime().ToString("O"),
            ["symbol"] = string.IsNullOrWhiteSpace(symbol) ? null : symbol.Trim(),
            ["sectorKey"] = string.IsNullOrWhiteSpace(sectorKey) ? null : sectorKey.Trim()
        };

        return await _apiClient.GetAsync<List<SecurityEarningsEventContract>>(
            $"{BasePath}/earnings/calendar",
            query,
            cancellationToken: cancellationToken)
            ?? [];
    }

    public async Task<PortfolioAnalyticsAccountSettingsContract> GetAccountSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await _apiClient.GetAsync<PortfolioAnalyticsAccountSettingsContract>(
            $"{BasePath}/settings/account",
            cancellationToken: cancellationToken)
            ?? new PortfolioAnalyticsAccountSettingsContract();
    }

    public async Task<PortfolioAnalyticsAccountSettingsContract> SaveAccountSettingsAsync(
        PortfolioAnalyticsAccountSettingsContract settings,
        CancellationToken cancellationToken = default)
    {
        return await _apiClient.PostAsync<PortfolioAnalyticsAccountSettingsContract, PortfolioAnalyticsAccountSettingsContract>(
            $"{BasePath}/settings/account",
            settings,
            cancellationToken: cancellationToken)
            ?? settings;
    }

    public async Task<PortfolioAnalyticsSystemSettingsContract> GetSystemSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await _apiClient.GetAsync<PortfolioAnalyticsSystemSettingsContract>(
            $"{BasePath}/settings/system",
            cancellationToken: cancellationToken)
            ?? new PortfolioAnalyticsSystemSettingsContract();
    }

    public async Task<PortfolioAnalyticsSystemSettingsContract> SaveSystemSettingsAsync(
        PortfolioAnalyticsSystemSettingsContract settings,
        CancellationToken cancellationToken = default)
    {
        return await _apiClient.PostAsync<PortfolioAnalyticsSystemSettingsContract, PortfolioAnalyticsSystemSettingsContract>(
            $"{BasePath}/settings/system",
            settings,
            cancellationToken: cancellationToken)
            ?? settings;
    }
}
