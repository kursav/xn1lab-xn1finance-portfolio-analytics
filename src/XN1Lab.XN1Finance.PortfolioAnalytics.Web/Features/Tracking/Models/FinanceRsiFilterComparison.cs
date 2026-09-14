namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;

/// <summary>Paired gross price outcomes from the same observed decision population, including Hold.</summary>
public sealed class FinanceRsiFilterComparison
{
    public string RecipeVersion { get; set; } = string.Empty;
    public DateTime ObservedThroughUtc { get; set; }
    public int DecisionCount { get; set; }
    public int DuplicateDecisionCount { get; set; }
    public int InvalidDecisionCount { get; set; }
    public int UnknownBaseDecisionCount { get; set; }
    public int BaseOpportunityCount { get; set; }
    public int ComparableOpportunityCount { get; set; }
    public int ExcludedOpportunityCount { get; set; }
    public int MissingIndicatorDecisionCount { get; set; }
    public int InvalidIndicatorDecisionCount { get; set; }
    public List<FinanceRsiFilterVariant> Variants { get; set; } = [];
}

public sealed class FinanceRsiFilterVariant
{
    public string Key { get; set; } = string.Empty;
    public int AcceptedOpportunityCount { get; set; }
    public int RejectedOpportunityCount { get; set; }
    public List<FinanceRsiOutcomeStatistics> Accepted { get; set; } = [];
    public List<FinanceRsiOutcomeStatistics> Rejected { get; set; } = [];
}

/// <summary>Measured raw price direction excludes unavailable outcomes; it is not net paper trade profit.</summary>
public sealed class FinanceRsiOutcomeStatistics
{
    public int HorizonMinutes { get; set; }
    public int Eligible { get; set; }
    public int Measured { get; set; }
    public int Positive { get; set; }
    public int Negative { get; set; }
    public int Neutral { get; set; }
    public int Pending { get; set; }
    public int Missing { get; set; }
    public int NoReference { get; set; }
    public int Invalid { get; set; }
    public decimal? PositiveRatePercent { get; set; }
    public decimal? CoveragePercent { get; set; }
    public decimal? AveragePriceChangePercent { get; set; }
    public int LiveQuoteCount { get; set; }
    public int RecordedDecisionCount { get; set; }
    public int UnknownProvenanceCount { get; set; }
}
