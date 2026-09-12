namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;

/// <summary>Versioned declarative configuration; never contains executable user code.</summary>
public sealed class TrackingPlanDefinition
{
    public int SchemaVersion { get; set; } = 1;
    public List<TrackingInstrument> Instruments { get; set; } = [];
    public List<TrackingIndicator> Indicators { get; set; } = [];
    public List<TrackingScoreComponent> ScoreComponents { get; set; } = [];
    public TrackingRule? EntryRule { get; set; }
    public TrackingRule? ExitRule { get; set; }
    public TrackingSchedule Schedule { get; set; } = new();
    public TrackingExecutionSettings Execution { get; set; } = new();
    public bool NotificationsEnabled { get; set; } = true;
}

public sealed class TrackingInstrument
{
    /// <summary>Canonical PortfolioAnalytics SecurityInstrument ID, not a provider ticker.</summary>
    public Guid InstrumentId { get; set; }
    public string MarketDataProviderKey { get; set; } = string.Empty;
    public Guid? MarketDataConnectionId { get; set; }
}

public sealed class TrackingIndicator
{
    public string BindingKey { get; set; } = string.Empty;
    public string IndicatorCode { get; set; } = string.Empty;
    public int IndicatorVersion { get; set; } = 1;
    public TrackingTimeframe Timeframe { get; set; } = TrackingTimeframe.Hour2;
    public TrackingPriceField Source { get; set; } = TrackingPriceField.Close;
    public Dictionary<string, decimal> Parameters { get; set; } = [];
}

public sealed class TrackingScoreComponent
{
    public string Key { get; set; } = string.Empty;
    /// <summary>Component weights total 100; independent of order budget percentage.</summary>
    public decimal WeightPercent { get; set; }
    public TrackingRule? Condition { get; set; }
    public decimal WhenTrueScore { get; set; } = 100;
    public decimal WhenFalseScore { get; set; }
}

public sealed class TrackingRule
{
    public TrackingRuleKind Kind { get; set; }
    public TrackingComparison? Comparison { get; set; }
    public TrackingValue? Left { get; set; }
    public TrackingValue? Right { get; set; }
    public List<TrackingRule> Children { get; set; } = [];
}

public sealed class TrackingValue
{
    public TrackingValueKind Kind { get; set; }
    public decimal? Constant { get; set; }
    public string? IndicatorBindingKey { get; set; }
    public string? OutputKey { get; set; }
    public TrackingTimeframe? Timeframe { get; set; }
    public TrackingPriceField? PriceField { get; set; }
}

public sealed class TrackingSchedule
{
    public TrackingTimeframe DecisionTimeframe { get; set; } = TrackingTimeframe.Hour2;
    public int PollIntervalSeconds { get; set; } = 60;
    public int SignalTtlSeconds { get; set; } = 300;
    public int CooldownSeconds { get; set; } = 7200;
    /// <summary>V1 only supports closed, available candles. Missing/stale data must skip evaluation.</summary>
    public bool ClosedCandlesOnly { get; set; } = true;
}

public sealed class TrackingExecutionSettings
{
    public TrackingExecutionMode Mode { get; set; } = TrackingExecutionMode.Observe;
    public string QuoteCurrency { get; set; } = "USD";
    public decimal PaperInitialBalance { get; set; } = 10000;
    public TrackingSizingKind SizingKind { get; set; } = TrackingSizingKind.AvailableBalancePercent;
    public decimal? FixedQuoteAmount { get; set; }
    public decimal? AvailableBalancePercent { get; set; } = 10;
    public decimal SellPositionPercent { get; set; } = 100;
    public TrackingOrderType OrderType { get; set; } = TrackingOrderType.Market;
    public decimal? LimitPrice { get; set; }
    public decimal? StopLossPercent { get; set; }
    public decimal? TakeProfitPercent { get; set; }
    public int MaxOpenPositions { get; set; } = 1;
    public decimal MaxPlanExposurePercent { get; set; } = 20;
    /// <summary>Paper simulation assumptions in basis points, not broker fee quotations.</summary>
    public decimal PaperFeeBps { get; set; } = 10;
    public decimal PaperSlippageBps { get; set; } = 5;
    /// <summary>Draft reference only. Activation requires an account-owned, verified trading integration.</summary>
    public Guid? BrokerConnectionId { get; set; }
}
