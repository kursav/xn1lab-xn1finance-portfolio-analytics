namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;

/// <summary>Descriptive results for decisions and paper positions opened in [FromUtc, ToUtc).</summary>
public sealed class FinanceStrategyResults
{
    public DateTime GeneratedAtUtc { get; set; }
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public List<FinanceStrategyResult> Plans { get; set; } = [];
}

public sealed class FinanceStrategyResult
{
    public Guid PlanId { get; set; }
    public Guid PlanVersionId { get; set; }
    public int VersionNumber { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public TrackingPlanStatus Status { get; set; }
    public TrackingExecutionMode Mode { get; set; }
    public string QuoteCurrency { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? FirstDecisionAtUtc { get; set; }
    public DateTime? LastDecisionAtUtc { get; set; }
    public int DecisionCount { get; set; }
    public int DirectionalDecisionCount { get; set; }
    public int UnavailableCount { get; set; }
    public List<FinanceHorizonStatistics> Direction { get; set; } = [];
    public FinancePaperPerformance Paper { get; set; } = new();
    public List<FinanceIndicatorPerformance> Indicators { get; set; } = [];
    public List<TrackingIndicator> IndicatorDefinitions { get; set; } = [];
}

/// <summary>Success means strictly positive action-aligned gross change. Neutral is measured but not a success.</summary>
public sealed class FinanceHorizonStatistics
{
    public int HorizonMinutes { get; set; }
    public int Eligible { get; set; }
    public int Measured { get; set; }
    public int Successes { get; set; }
    public int Failures { get; set; }
    public int Neutral { get; set; }
    public int Pending { get; set; }
    public int Missing { get; set; }
    public int NoReference { get; set; }
    public decimal? SuccessRatePercent { get; set; }
    public decimal? CoveragePercent { get; set; }
    public decimal? AverageAlignedChangePercent { get; set; }
}

/// <summary>Closed episodes group all partial fills until flat; fees and simulated slippage are included.</summary>
public sealed class FinancePaperPerformance
{
    public int ClosedEpisodeCount { get; set; }
    public int OpenEpisodeCount { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Breakeven { get; set; }
    public decimal NetPnl { get; set; }
    public decimal? WinRatePercent { get; set; }
    public decimal? AverageNetPnl { get; set; }
    public decimal? AverageReturnPercent { get; set; }
    public decimal? AverageWin { get; set; }
    public decimal? AverageLoss { get; set; }
    public decimal? ProfitFactor { get; set; }
    public decimal? AverageHoldingMinutes { get; set; }
    public decimal ClosedEpisodeFees { get; set; }
    public int UnmatchedFillCount { get; set; }
    public List<FinanceExitPerformance> Exits { get; set; } = [];
}

public sealed class FinanceExitPerformance
{
    public string Reason { get; set; } = string.Empty;
    public int ClosedEpisodeCount { get; set; }
    public int Wins { get; set; }
    public decimal NetPnl { get; set; }
}

/// <summary>Conditional component outcomes, not an indicator's independent causal contribution.</summary>
public sealed class FinanceIndicatorPerformance
{
    public string ComponentKey { get; set; } = string.Empty;
    public decimal WeightPercent { get; set; }
    public List<string> Bindings { get; set; } = [];
    public int MatchedDecisionCount { get; set; }
    public int MatchedDirectionalDecisionCount { get; set; }
    public List<FinanceHorizonStatistics> Direction { get; set; } = [];
}
