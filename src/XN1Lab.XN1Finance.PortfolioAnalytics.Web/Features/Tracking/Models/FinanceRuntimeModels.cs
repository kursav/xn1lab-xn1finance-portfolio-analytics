using System.Text.Json.Serialization;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;

/// <summary>Provider-neutral OHLCV. CloseTimeUtc is the exclusive UTC candle boundary.</summary>
public sealed record FinanceCandle(DateTime OpenTimeUtc, DateTime CloseTimeUtc,
    decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);

/// <summary>Last traded price; AsOfUtc is the exchange timestamp, ReceivedAtUtc is local receipt time.</summary>
public sealed record FinanceQuote(string ProviderKey, string ProviderSymbol, decimal Price,
    string Currency, DateTime AsOfUtc, DateTime ReceivedAtUtc);

[JsonConverter(typeof(JsonStringEnumConverter<FinanceDecisionAction>))]
public enum FinanceDecisionAction { Hold, Buy, Sell }
[JsonConverter(typeof(JsonStringEnumConverter<FinanceEvaluationKind>))]
public enum FinanceEvaluationKind { Decision, Protection, Unavailable }

public sealed class FinanceEvaluationDecision
{
    public bool IsAvailable { get; set; }
    public string? UnavailableReason { get; set; }
    public decimal? Score { get; set; }
    public bool EntryMatched { get; set; }
    public bool ExitMatched { get; set; }
    public FinanceDecisionAction Action { get; set; }
    public DateTime DecisionTimeUtc { get; set; }
    public string DataFingerprint { get; set; } = string.Empty;
    public List<FinanceScoreContribution> Contributions { get; set; } = [];
    public List<FinanceIndicatorValue> IndicatorValues { get; set; } = [];
    public FinanceQuote? ReferenceQuote { get; set; }
}

public sealed record FinanceScoreContribution(string Key, bool Matched, decimal WeightPercent, decimal SelectedScore, decimal Contribution);
public sealed record FinanceIndicatorValue(string BindingKey, string OutputKey, decimal CurrentValue, decimal PreviousValue);

public sealed class FinanceEvaluation
{
    public Guid Id { get; set; }
    public Guid OwnerAccountId { get; set; }
    public Guid PlanId { get; set; }
    public Guid PlanVersionId { get; set; }
    public long ExecutionGeneration { get; set; }
    public Guid InstrumentId { get; set; }
    public FinanceEvaluationKind Kind { get; set; }
    public DateTime DecisionTimeUtc { get; set; }
    public DateTime EvaluatedAtUtc { get; set; }
    public FinanceEvaluationDecision Decision { get; set; } = new();
    public string? ExecutionReason { get; set; }
    public List<FinanceForwardOutcome> ForwardOutcomes { get; set; } = [];
}

public sealed record FinanceForwardOutcome(int HorizonMinutes, string Status, DateTime? DueAtUtc,
    DateTime? ObservedAtUtc = null, decimal? Price = null, decimal? PriceChangePercent = null,
    decimal? ActionAlignedChangePercent = null);

public sealed class FinanceSignal
{
    public Guid Id { get; set; }
    public Guid EvaluationId { get; set; }
    public Guid OwnerAccountId { get; set; }
    public Guid PlanId { get; set; }
    public Guid PlanVersionId { get; set; }
    public long ExecutionGeneration { get; set; }
    public Guid InstrumentId { get; set; }
    public TrackingExecutionMode Mode { get; set; }
    public FinanceDecisionAction Action { get; set; }
    public decimal? Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class FinanceNotification
{
    public Guid Id { get; set; }
    public Guid SignalId { get; set; }
    public Guid OwnerAccountId { get; set; }
    public Guid PlanId { get; set; }
    public Guid InstrumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}

public sealed class FinancePaperWallet
{
    public Guid PlanId { get; set; }
    public Guid OwnerAccountId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public decimal CashBalance { get; set; }
    public decimal RealizedPnl { get; set; }
    public decimal TotalFees { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class FinancePaperPosition
{
    public Guid PlanId { get; set; }
    public Guid OwnerAccountId { get; set; }
    public Guid InstrumentId { get; set; }
    public decimal Quantity { get; set; }
    /// <summary>Cost per unit including the entry fee.</summary>
    public decimal AverageCost { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal? StopLossPrice { get; set; }
    public decimal? TakeProfitPrice { get; set; }
    public decimal LastMarkPrice { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class FinancePaperTrade
{
    public Guid Id { get; set; }
    public Guid EvaluationId { get; set; }
    public Guid SignalId { get; set; }
    public Guid OwnerAccountId { get; set; }
    public Guid PlanId { get; set; }
    public Guid InstrumentId { get; set; }
    public FinanceDecisionAction Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal FillPrice { get; set; }
    public decimal Fee { get; set; }
    public decimal CashDelta { get; set; }
    public decimal RealizedPnl { get; set; }
    public decimal CashBalanceAfter { get; set; }
    public string SimulationModel { get; set; } = "LastTradeWithSlippageV1";
    public DateTime ExecutedAtUtc { get; set; }
}

public sealed class FinancePaperPortfolio
{
    public FinancePaperWallet? Wallet { get; set; }
    public List<FinancePaperPosition> Positions { get; set; } = [];
}

public sealed class FinancePreviewResult
{
    public Guid PlanId { get; set; }
    public Guid InstrumentId { get; set; }
    public FinanceQuote? Quote { get; set; }
    public FinanceEvaluationDecision Decision { get; set; } = new();
}
