using XN1Lab.Platform.CoreServices.Contracts.PortfolioAnalytics;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;

public sealed record FinanceMarketDataCapabilities
{
    public required string ProviderKey { get; init; }
    public required string DisplayName { get; init; }
    public bool IsPublic { get; init; }
    public bool SupportsInstrumentLookup { get; init; }
    public bool SupportsHistoricalCandles { get; init; }
    public bool SupportsLatestQuote { get; init; }
    public int MaxCandlesPerRequest { get; init; }
    public IReadOnlyList<PortfolioInstrumentType> InstrumentTypes { get; init; } = [];
    public IReadOnlyList<TrackingTimeframe> Timeframes { get; init; } = [];
}
