namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.PortfolioAnalytics.Models;

public sealed class PortfolioAnalyticsDashboardSnapshot
{
    public int PortfolioCount { get; init; }
    public int HoldingCount { get; init; }
    public decimal EstimatedValue { get; init; }
    public string BaseCurrency { get; init; } = "USD";
    public IReadOnlyList<PortfolioAnalyticsReadinessItem> ReadinessItems { get; init; } = [];
}

public sealed class PortfolioAnalyticsReadinessItem
{
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Icon { get; init; } = "circle outline";
}

