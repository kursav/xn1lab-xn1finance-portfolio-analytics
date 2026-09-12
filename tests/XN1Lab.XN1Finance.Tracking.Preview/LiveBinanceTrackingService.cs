using System.Globalization;
using System.Text.Json;
using XN1Lab.Platform.Shared.Pagination;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;
using XN1Lab.XN1Finance.Tracking.Tests.Fixtures;

namespace XN1Lab.XN1Finance.Tracking.Preview;

/// <summary>
/// Keeps automated tests deterministic while making the running preview honest:
/// price and indicators come from Binance's public market-data-only API.
/// </summary>
internal sealed class LiveBinanceTrackingService(HttpClient client) : TrackingFixtureService
{
    private readonly SemaphoreSlim _snapshotLock = new(1, 1);
    private MarketSnapshot? _cachedSnapshot;

    public override async Task<IReadOnlyList<FinancePreviewResult>> PreviewAsync(
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        Calls.Add("preview");
        var snapshot = await LoadSnapshotAsync(cancellationToken);

        return
        [
            new FinancePreviewResult
            {
                PlanId = planId,
                InstrumentId = InstrumentId,
                Quote = new FinanceQuote(
                    "binance-spot",
                    "BTCUSDT",
                    snapshot.Price,
                    "USDT",
                    snapshot.ReceivedAtUtc,
                    snapshot.ReceivedAtUtc),
                Decision = BuildDecision(snapshot)
            }
        ];
    }

    public override async Task<PaginatedList<FinanceEvaluation>> GetEvaluationsAsync(
        Guid planId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await LoadSnapshotAsync(cancellationToken);
        var decision = BuildDecision(snapshot);

        return Page(
        [
            new FinanceEvaluation
            {
                Id = Guid.NewGuid(),
                PlanId = planId,
                PlanVersionId = Plan.CurrentVersionId,
                InstrumentId = InstrumentId,
                Kind = FinanceEvaluationKind.Decision,
                Decision = decision,
                EvaluatedAtUtc = snapshot.ReceivedAtUtc,
                DecisionTimeUtc = decision.DecisionTimeUtc,
                ExecutionReason = "preview_only"
            }
        ], pageNumber);
    }

    public override Task<PaginatedList<FinanceSignal>> GetSignalsAsync(
        Guid planId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Page(Array.Empty<FinanceSignal>(), pageNumber));

    public override async Task<FinancePaperPortfolio> GetPaperAsync(
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await LoadSnapshotAsync(cancellationToken);
        var entryPrice = decimal.Round(snapshot.Price * 0.98m, 2);
        var averageCost = decimal.Round(entryPrice * 1.001m, 2);
        var quantity = decimal.Round(1000m / entryPrice, 8);

        return new FinancePaperPortfolio
        {
            Wallet = new FinancePaperWallet
            {
                PlanId = planId,
                Currency = "USDT",
                InitialBalance = 10000m,
                CashBalance = 9000m,
                TotalFees = 1m,
                CreatedAtUtc = snapshot.ReceivedAtUtc.AddDays(-2),
                UpdatedAtUtc = snapshot.ReceivedAtUtc
            },
            Positions =
            [
                new FinancePaperPosition
                {
                    PlanId = planId,
                    InstrumentId = InstrumentId,
                    Quantity = quantity,
                    EntryPrice = entryPrice,
                    AverageCost = averageCost,
                    LastMarkPrice = snapshot.Price,
                    StopLossPrice = decimal.Round(entryPrice * 0.97m, 2),
                    TakeProfitPrice = decimal.Round(entryPrice * 1.06m, 2),
                    UpdatedAtUtc = snapshot.ReceivedAtUtc
                }
            ]
        };
    }

    public override async Task<PaginatedList<FinancePaperTrade>> GetPaperTradesAsync(
        Guid planId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await LoadSnapshotAsync(cancellationToken);
        var fillPrice = decimal.Round(snapshot.Price * 0.98m, 2);
        var quantity = decimal.Round(1000m / fillPrice, 8);

        return Page(
        [
            new FinancePaperTrade
            {
                Id = Guid.NewGuid(),
                PlanId = planId,
                InstrumentId = InstrumentId,
                Side = FinanceDecisionAction.Buy,
                Quantity = quantity,
                FillPrice = fillPrice,
                Fee = 1m,
                CashDelta = -1000m,
                CashBalanceAfter = 9000m,
                ExecutedAtUtc = snapshot.ReceivedAtUtc.AddHours(-2)
            }
        ], pageNumber);
    }

    private async Task<MarketSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        var cached = _cachedSnapshot;
        if (cached is not null && DateTime.UtcNow - cached.ReceivedAtUtc < TimeSpan.FromSeconds(15))
        {
            return cached;
        }

        await _snapshotLock.WaitAsync(cancellationToken);
        try
        {
            cached = _cachedSnapshot;
            if (cached is not null && DateTime.UtcNow - cached.ReceivedAtUtc < TimeSpan.FromSeconds(15))
            {
                return cached;
            }

            var tickerTask = client.GetStringAsync("api/v3/ticker/price?symbol=BTCUSDT", cancellationToken);
            var rsiCandlesTask = client.GetStringAsync("api/v3/klines?symbol=BTCUSDT&interval=2h&limit=250", cancellationToken);
            var emaCandlesTask = client.GetStringAsync("api/v3/klines?symbol=BTCUSDT&interval=1d&limit=250", cancellationToken);
            await Task.WhenAll(tickerTask, rsiCandlesTask, emaCandlesTask);

            var receivedAtUtc = DateTime.UtcNow;
            var price = ParseTicker(await tickerTask);
            var twoHourCandles = ParseClosedCandles(await rsiCandlesTask, receivedAtUtc);
            var dailyCandles = ParseClosedCandles(await emaCandlesTask, receivedAtUtc);
            var (currentRsi, previousRsi) = CalculateRsi(twoHourCandles.Select(x => x.Close).ToArray(), 14);
            var (currentEma, previousEma) = CalculateEma(dailyCandles.Select(x => x.Close).ToArray(), 50);

            _cachedSnapshot = new MarketSnapshot(
                price,
                currentRsi,
                previousRsi,
                currentEma,
                previousEma,
                dailyCandles[^1].Close,
                twoHourCandles[^1].CloseTimeUtc,
                receivedAtUtc);

            return _cachedSnapshot;
        }
        finally
        {
            _snapshotLock.Release();
        }
    }

    private static FinanceEvaluationDecision BuildDecision(MarketSnapshot snapshot)
    {
        var momentumMatched = snapshot.CurrentRsi < 30m;
        var trendMatched = snapshot.LastDailyClose > 50000m;
        var momentumContribution = momentumMatched ? 60m : 0m;
        var trendContribution = trendMatched ? 40m : 0m;
        var score = momentumContribution + trendContribution;
        var entryMatched = score >= 75m;
        var exitMatched = score <= 25m;

        return new FinanceEvaluationDecision
        {
            IsAvailable = true,
            Score = score,
            EntryMatched = entryMatched,
            ExitMatched = exitMatched,
            Action = entryMatched
                ? FinanceDecisionAction.Buy
                : exitMatched
                    ? FinanceDecisionAction.Sell
                    : FinanceDecisionAction.Hold,
            DecisionTimeUtc = snapshot.DecisionTimeUtc,
            DataFingerprint = $"binance-live:{snapshot.DecisionTimeUtc:O}",
            Contributions =
            [
                new FinanceScoreContribution("momentum", momentumMatched, 60m, momentumMatched ? 100m : 0m, momentumContribution),
                new FinanceScoreContribution("trend", trendMatched, 40m, trendMatched ? 100m : 0m, trendContribution)
            ],
            IndicatorValues =
            [
                new FinanceIndicatorValue("rsi2h", "value", snapshot.CurrentRsi, snapshot.PreviousRsi),
                new FinanceIndicatorValue("emaDaily", "value", snapshot.CurrentEma, snapshot.PreviousEma)
            ]
        };
    }

    private static decimal ParseTicker(string json)
    {
        using var document = JsonDocument.Parse(json);
        var value = document.RootElement.GetProperty("price").GetString();
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
        {
            throw new InvalidDataException("Binance BTCUSDT fiyatı okunamadı.");
        }

        return price;
    }

    private static List<ClosedCandle> ParseClosedCandles(string json, DateTime receivedAtUtc)
    {
        var nowMilliseconds = new DateTimeOffset(receivedAtUtc).ToUnixTimeMilliseconds();
        using var document = JsonDocument.Parse(json);
        var candles = new List<ClosedCandle>();

        foreach (var row in document.RootElement.EnumerateArray())
        {
            var closeTimeMilliseconds = row[6].GetInt64();
            if (closeTimeMilliseconds > nowMilliseconds)
            {
                continue;
            }

            var closeText = row[4].GetString();
            if (!decimal.TryParse(closeText, NumberStyles.Number, CultureInfo.InvariantCulture, out var close))
            {
                throw new InvalidDataException("Binance mum kapanış fiyatı okunamadı.");
            }

            candles.Add(new ClosedCandle(
                DateTimeOffset.FromUnixTimeMilliseconds(closeTimeMilliseconds).UtcDateTime,
                close));
        }

        if (candles.Count < 51)
        {
            throw new InvalidDataException("İndikatör hesabı için yeterli kapanmış Binance mumu alınamadı.");
        }

        return candles;
    }

    private static (decimal Current, decimal Previous) CalculateRsi(IReadOnlyList<decimal> closes, int period)
    {
        if (closes.Count <= period + 1)
        {
            throw new ArgumentException("RSI için yeterli kapanış yok.", nameof(closes));
        }

        decimal averageGain = 0m;
        decimal averageLoss = 0m;
        for (var index = 1; index <= period; index++)
        {
            var change = closes[index] - closes[index - 1];
            averageGain += Math.Max(change, 0m);
            averageLoss += Math.Max(-change, 0m);
        }

        averageGain /= period;
        averageLoss /= period;
        var current = ToRsi(averageGain, averageLoss);
        var previous = current;

        for (var index = period + 1; index < closes.Count; index++)
        {
            var change = closes[index] - closes[index - 1];
            averageGain = ((averageGain * (period - 1)) + Math.Max(change, 0m)) / period;
            averageLoss = ((averageLoss * (period - 1)) + Math.Max(-change, 0m)) / period;
            previous = current;
            current = ToRsi(averageGain, averageLoss);
        }

        return (decimal.Round(current, 4), decimal.Round(previous, 4));
    }

    private static decimal ToRsi(decimal averageGain, decimal averageLoss)
    {
        if (averageGain == 0m && averageLoss == 0m)
        {
            return 50m;
        }

        if (averageLoss == 0m)
        {
            return 100m;
        }

        if (averageGain == 0m)
        {
            return 0m;
        }

        return 100m - (100m / (1m + (averageGain / averageLoss)));
    }

    private static (decimal Current, decimal Previous) CalculateEma(IReadOnlyList<decimal> closes, int period)
    {
        if (closes.Count <= period)
        {
            throw new ArgumentException("EMA için yeterli kapanış yok.", nameof(closes));
        }

        var ema = closes.Take(period).Average();
        var previous = ema;
        var multiplier = 2m / (period + 1m);

        for (var index = period; index < closes.Count; index++)
        {
            previous = ema;
            ema = ((closes[index] - ema) * multiplier) + ema;
        }

        return (decimal.Round(ema, 4), decimal.Round(previous, 4));
    }

    private sealed record ClosedCandle(DateTime CloseTimeUtc, decimal Close);

    private sealed record MarketSnapshot(
        decimal Price,
        decimal CurrentRsi,
        decimal PreviousRsi,
        decimal CurrentEma,
        decimal PreviousEma,
        decimal LastDailyClose,
        DateTime DecisionTimeUtc,
        DateTime ReceivedAtUtc);
}
