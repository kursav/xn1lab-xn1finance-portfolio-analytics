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
                    new() { BindingKey = "ema5", IndicatorCode = "EMA", Timeframe = TrackingTimeframe.Minute5, Parameters = new() { ["period"] = 9 } }
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
                Mode = TrackingExecutionMode.Paper, QuoteCurrency = "USDT", CreatedAtUtc = fromUtc
            }
        ]
    };
}
