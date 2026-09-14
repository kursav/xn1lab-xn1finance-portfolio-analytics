using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;

namespace XN1Lab.XN1Finance.Tracking.Tests.Fixtures;

// Explicit samples for component tests and the fixture preview; never registered by the real API client.
public static class StrategyResultsFixture
{
    public static readonly Guid PlanId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid VersionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public static FinanceStrategyResults Create(DateTime fromUtc, DateTime toUtc) => new()
    {
        GeneratedAtUtc = toUtc, FromUtc = fromUtc, ToUtc = toUtc,
        Plans =
        [
            new()
            {
                PlanId = PlanId, PlanVersionId = VersionId, VersionNumber = 2,
                PlanName = "Örnek · RSI toparlanma", Status = TrackingPlanStatus.Active,
                Mode = TrackingExecutionMode.Paper, QuoteCurrency = "USDT",
                CreatedAtUtc = fromUtc, DecisionCount = 100, DirectionalDecisionCount = 10, UnavailableCount = 2,
                RsiFilterComparison = CreateComparison(toUtc),
                Direction =
                [
                    new() { HorizonMinutes = 5, Eligible = 10, Measured = 8, Successes = 5, Failures = 2, Neutral = 1, Pending = 1, Missing = 1, SuccessRatePercent = 62.5m, CoveragePercent = 80 },
                    new() { HorizonMinutes = 15, Eligible = 10, Measured = 6, Successes = 3, Failures = 3, Pending = 2, Missing = 1, NoReference = 1, SuccessRatePercent = 50, CoveragePercent = 60 },
                    new() { HorizonMinutes = 30, Eligible = 10, Pending = 7, Missing = 2, NoReference = 1 }
                ],
                Paper = new()
                {
                    ClosedEpisodeCount = 4, OpenEpisodeCount = 1, Wins = 1, Losses = 2, Breakeven = 1,
                    NetPnl = -3.75m, WinRatePercent = 25, AverageNetPnl = -0.9375m,
                    AverageReturnPercent = -0.09375m, AverageHoldingMinutes = 21.5m, ClosedEpisodeFees = 3,
                    Exits = [new() { Reason = "paper_max_holding_time", ClosedEpisodeCount = 3, Wins = 1, NetPnl = -2.75m }, new() { Reason = "paper_trailing_stop", ClosedEpisodeCount = 1, NetPnl = -1 }]
                },
                IndicatorDefinitions =
                [
                    new() { BindingKey = "rsi5", IndicatorCode = "RSI", Timeframe = TrackingTimeframe.Minute5, Parameters = new() { ["period"] = 14 } },
                    new() { BindingKey = "rsi1h", IndicatorCode = "RSI", Timeframe = TrackingTimeframe.Hour1, Parameters = new() { ["period"] = 14 } },
                    new() { BindingKey = "rsi2h", IndicatorCode = "RSI", Timeframe = TrackingTimeframe.Hour2, Parameters = new() { ["period"] = 14 } }
                ],
                Indicators = [new()
                {
                    ComponentKey = "momentum", WeightPercent = 60, Bindings = ["rsi5"], MatchedDecisionCount = 20, MatchedDirectionalDecisionCount = 8,
                    Direction = [new() { HorizonMinutes = 5, Eligible = 8, Measured = 6, Successes = 4, Failures = 2, Pending = 1, Missing = 1, SuccessRatePercent = 66.666667m, CoveragePercent = 75 }]
                }]
            },
            new()
            {
                PlanId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                PlanVersionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), VersionNumber = 1,
                PlanName = "Örnek · EMA hız", Status = TrackingPlanStatus.Active,
                Mode = TrackingExecutionMode.Paper, QuoteCurrency = "USDT", CreatedAtUtc = fromUtc,
                IndicatorDefinitions = [new() { BindingKey = "ema5", IndicatorCode = "EMA", Timeframe = TrackingTimeframe.Minute5, Parameters = new() { ["period"] = 9 } }]
            }
        ]
    };

    public static FinanceRsiFilterComparison CreateComparison(DateTime observedThroughUtc)
    {
        var comparison = new FinanceRsiFilterComparison
        {
            RecipeVersion = "rsi14-5m-cross50-max65-hour1-hour2-v1", ObservedThroughUtc = observedThroughUtc,
            DecisionCount = 100, DuplicateDecisionCount = 1, InvalidDecisionCount = 1,
            UnknownBaseDecisionCount = 2, BaseOpportunityCount = 12, ComparableOpportunityCount = 10,
            ExcludedOpportunityCount = 2, MissingIndicatorDecisionCount = 3, InvalidIndicatorDecisionCount = 1
        };
        foreach (var key in new[] { "none", "hour1", "hour2", "both" })
        {
            bool Accept(int index) => key switch { "hour1" => index < 6, "hour2" => index % 2 == 0, "both" => index < 6 && index % 2 == 0, _ => true };
            var accepted = Enumerable.Range(0, 10).Where(Accept).ToArray();
            var rejected = Enumerable.Range(0, 10).Where(index => !Accept(index)).ToArray();
            comparison.Variants.Add(new()
            {
                Key = key, AcceptedOpportunityCount = accepted.Length, RejectedOpportunityCount = rejected.Length,
                Accepted = [.. new[] { 5, 15, 30 }.Select(horizon => Outcomes(horizon, accepted))],
                Rejected = [.. new[] { 5, 15, 30 }.Select(horizon => Outcomes(horizon, rejected))]
            });
        }
        return comparison;
    }

    private static FinanceRsiOutcomeStatistics Outcomes(int horizon, int[] opportunities)
    {
        decimal[] changes = [0.2m, -0.1m, 0.1m, -0.2m, 0.3m, -0.05m, 0.15m, 0m, 0m, 0m];
        string[] states = horizon switch
        {
            5 => ["measured", "measured", "measured", "measured", "measured", "measured", "measured", "measured", "pending", "missing"],
            15 => ["measured", "measured", "measured", "measured", "measured", "measured", "pending", "pending", "missing", "no_reference"],
            _ => ["measured", "measured", "measured", "pending", "missing", "no_reference", "invalid", "pending", "missing", "pending"]
        };
        var measured = opportunities.Where(index => states[index] == "measured").ToArray();
        var positive = measured.Count(index => changes[index] > 0);
        return new()
        {
            HorizonMinutes = horizon, Eligible = opportunities.Length, Measured = measured.Length,
            Positive = positive, Negative = measured.Count(index => changes[index] < 0), Neutral = measured.Count(index => changes[index] == 0),
            Pending = opportunities.Count(index => states[index] == "pending"), Missing = opportunities.Count(index => states[index] == "missing"),
            NoReference = opportunities.Count(index => states[index] == "no_reference"), Invalid = opportunities.Count(index => states[index] == "invalid"),
            PositiveRatePercent = measured.Length > 0 ? 100m * positive / measured.Length : null,
            CoveragePercent = opportunities.Length > 0 ? 100m * measured.Length / opportunities.Length : null,
            AveragePriceChangePercent = measured.Length > 0 ? measured.Average(index => changes[index]) : null,
            LiveQuoteCount = measured.Count(index => index < 3), RecordedDecisionCount = measured.Count(index => index >= 3 && index < 6),
            UnknownProvenanceCount = measured.Count(index => index >= 6)
        };
    }
}
