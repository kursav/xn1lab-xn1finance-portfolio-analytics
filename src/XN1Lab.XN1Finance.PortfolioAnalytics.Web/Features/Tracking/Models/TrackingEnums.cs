using System.Text.Json.Serialization;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;

[JsonConverter(typeof(JsonStringEnumConverter<TrackingPlanStatus>))]
public enum TrackingPlanStatus { Draft, Active, Paused, Archived }
[JsonConverter(typeof(JsonStringEnumConverter<TrackingExecutionMode>))]
public enum TrackingExecutionMode { Observe, Paper, Live }
[JsonConverter(typeof(JsonStringEnumConverter<TrackingTimeframe>))]
public enum TrackingTimeframe { Minute5, Minute15, Minute30, Hour1, Hour2, Hour4, Day1, Week1 }
[JsonConverter(typeof(JsonStringEnumConverter<TrackingRuleKind>))]
public enum TrackingRuleKind { Comparison, All, Any, Not }
[JsonConverter(typeof(JsonStringEnumConverter<TrackingComparison>))]
public enum TrackingComparison { LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual, CrossesAbove, CrossesBelow }
[JsonConverter(typeof(JsonStringEnumConverter<TrackingValueKind>))]
public enum TrackingValueKind { Constant, Indicator, Price, TotalScore }
[JsonConverter(typeof(JsonStringEnumConverter<TrackingPriceField>))]
public enum TrackingPriceField { Open, High, Low, Close, Volume }
[JsonConverter(typeof(JsonStringEnumConverter<TrackingSizingKind>))]
public enum TrackingSizingKind { FixedQuoteAmount, AvailableBalancePercent }
[JsonConverter(typeof(JsonStringEnumConverter<TrackingOrderType>))]
public enum TrackingOrderType { Market, Limit }
