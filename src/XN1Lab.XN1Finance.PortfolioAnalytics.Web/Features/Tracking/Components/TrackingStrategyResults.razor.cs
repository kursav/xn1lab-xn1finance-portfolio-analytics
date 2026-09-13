using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Components;
using XN1Lab.Platform.Api.Exceptions;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Services;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Components;

public partial class TrackingStrategyResults : IDisposable
{
    // A small-sample cue, not a confidence threshold or evidence of predictive reliability.
    private const int MinimumSampleSize = 20;
    private static readonly int[] Horizons = [5, 15, 30];
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private readonly CancellationTokenSource lifetime = new();
    private bool disposed;
    private bool Loading;
    private string? Error;
    private int Days = 7;
    private Guid? ExpandedVersionId;
    private FinanceStrategyResults? Results;

    [Inject] private IFinanceTrackingService Service { get; set; } = default!;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        if (Loading || disposed) return;
        Loading = true;
        Error = null;
        Results = null;
        ExpandedVersionId = null;
        try
        {
            var fromUtc = DateTime.UtcNow.AddDays(-Days);
            var results = await Service.GetStrategyResultsAsync(fromUtc, cancellationToken: lifetime.Token);
            if (!disposed) Results = results;
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception exception)
        {
            if (!disposed) Error = exception switch
            {
                PlatformApiException api when api.StatusCode == HttpStatusCode.BadRequest => "Bu kapsam için sonuçlar alınamadı. Daha az plan veya daha kısa dönem gerekebilir.",
                PlatformApiException api when api.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => "Sonuçları görmek için oturumunu ve erişim yetkini kontrol et.",
                _ => "Sonuçlar yüklenemedi. Yenile düğmesiyle tekrar dene."
            };
        }
        finally { if (!disposed) Loading = false; }
    }

    private void Toggle(Guid versionId) => ExpandedVersionId = ExpandedVersionId == versionId ? null : versionId;
    private RenderFragment DirectionRate(FinanceHorizonStatistics? data) => builder =>
    {
        builder.OpenComponent<TrackingResultRate>(0);
        builder.AddAttribute(1, nameof(TrackingResultRate.Favourable), (long)(data?.Successes ?? 0));
        builder.AddAttribute(2, nameof(TrackingResultRate.Measured), (long)(data?.Measured ?? 0));
        builder.AddAttribute(3, nameof(TrackingResultRate.Pending), (long)(data?.Pending ?? 0));
        builder.AddAttribute(4, nameof(TrackingResultRate.Missing), (long)(data?.Missing ?? 0));
        builder.AddAttribute(5, nameof(TrackingResultRate.NoReference), (long)(data?.NoReference ?? 0));
        builder.AddAttribute(6, nameof(TrackingResultRate.Eligible), (long)(data?.Eligible ?? 0));
        builder.AddAttribute(7, nameof(TrackingResultRate.Rate), data?.SuccessRatePercent);
        builder.AddAttribute(8, nameof(TrackingResultRate.MinimumSampleSize), MinimumSampleSize);
        builder.CloseComponent();
    };

    private static string Number(decimal? value) => value?.ToString("0.##", Turkish) ?? "—";
    private static string Rate(decimal? value) => value.HasValue ? $"%{Number(value)}" : "—";
    private static string Money(decimal? value, string currency) => value.HasValue ? $"{value.Value.ToString("N2", Turkish)} {currency}" : "—";
    private static string Utc(DateTime value) => value.ToString("dd.MM.yyyy HH:mm", Turkish);
    private static string Status(TrackingPlanStatus value) => value switch { TrackingPlanStatus.Active => "Açık", TrackingPlanStatus.Paused => "Durdu", TrackingPlanStatus.Archived => "Arşiv", _ => "Taslak" };
    private static string Mode(TrackingExecutionMode value) => value switch { TrackingExecutionMode.Paper => "Sanal", TrackingExecutionMode.Observe => "İzleme", _ => "Gerçek" };
    private static string Indicator(TrackingIndicator binding) => binding.Parameters.Count == 0 ? binding.IndicatorCode
        : $"{binding.IndicatorCode} ({string.Join(", ", binding.Parameters.Select(x => x.Key == "period" ? Number(x.Value) : $"{x.Key}: {Number(x.Value)}"))})";
    private static string ExitReason(string reason) => reason switch
    {
        "paper_trailing_stop" => "İz süren stop", "paper_max_holding_time" => "Süre doldu",
        "paper_stop_loss" => "Zarar durdur", "paper_take_profit" => "Kâr al",
        "paper_sell_filled" => "Satış koşulu", _ => reason
    };

    public void Dispose() { disposed = true; lifetime.Cancel(); lifetime.Dispose(); }
}
