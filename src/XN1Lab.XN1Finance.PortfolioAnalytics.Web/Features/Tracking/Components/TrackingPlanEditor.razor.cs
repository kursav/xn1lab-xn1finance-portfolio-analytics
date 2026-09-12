using System.Net;
using Microsoft.AspNetCore.Components;
using XN1Lab.Platform.Api.Exceptions;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Services;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Components;

public partial class TrackingPlanEditor : IDisposable
{
    [Inject] private IFinanceTrackingService Service { get; set; } = default!;
    [Parameter] public TrackingPlan? Plan { get; set; }
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public EventCallback<TrackingPlan> Saved { get; set; }
    [Parameter] public EventCallback Cancelled { get; set; }

    private readonly CancellationTokenSource lifetime = new();
    private readonly Dictionary<Guid, string> instrumentNames = [];
    private TrackingPlanDefinition Definition = TrackingEditorRules.NewDefinition();
    private IReadOnlyList<IndicatorDefinition> Catalog = [];
    private IReadOnlyList<FinanceMarketDataCapabilities> Providers = [];
    private TrackingCapabilities Capabilities = new();
    private TrackingPlanValidationResult? Validation;
    private string Name = "", Description = "", ImportProvider = "binance-spot", ImportSymbol = "BTCUSDT";
    private string? Error;
    private bool Loading = true, MetadataFailed, Saving, Validating, Importing, Initialized, WalletFunded;
    private Guid? loadedPlanId;
    private bool Busy => Loading || Saving || Validating || Importing;
    private bool Disabled => Busy || ReadOnly || MetadataFailed || Plan?.Status == TrackingPlanStatus.Archived
        || Plan is not null && Plan.CurrentVersion is null || Definition.SchemaVersion != 1;
    private decimal WeightTotal => Definition.ScoreComponents.Sum(x => x.WeightPercent);
    private bool WeightValid => Math.Abs(WeightTotal - 100) <= 0.0001m;

    protected override async Task OnParametersSetAsync()
    {
        // Parent refreshes must not replace unsaved edits or silently advance the expected revision.
        if (Initialized && loadedPlanId == Plan?.Id) return;
        Initialized = true;
        loadedPlanId = Plan?.Id;
        expectedRevision = Plan?.Revision;
        WalletFunded = false;
        instrumentNames.Clear();
        Name = Plan?.Name ?? "";
        Description = Plan?.Description ?? "";
        Definition = Plan?.CurrentVersion is { } version
            ? TrackingEditorRules.Clone(version.Definition) : TrackingEditorRules.NewDefinition();
        Validation = null;
        Error = null;
        await LoadMetadata();
    }

    private async Task LoadMetadata()
    {
        if (Saving || Validating || Importing) return;
        Loading = true;
        MetadataFailed = false;
        Error = null;
        try
        {
            var catalogTask = Service.GetIndicatorsAsync(lifetime.Token);
            var providersTask = Service.GetProvidersAsync(lifetime.Token);
            var capabilitiesTask = Service.GetCapabilitiesAsync(lifetime.Token);
            await Task.WhenAll(catalogTask, providersTask, capabilitiesTask);
            Catalog = await catalogTask;
            Providers = await providersTask;
            Capabilities = await capabilitiesTask;
            if (!Providers.Any(x => x.ProviderKey == ImportProvider && x.SupportsInstrumentLookup))
                ImportProvider = Providers.FirstOrDefault(x => x.SupportsInstrumentLookup)?.ProviderKey ?? "";
            if (Plan is not null)
            {
                var paper = await Service.GetPaperAsync(Plan.Id, lifetime.Token);
                WalletFunded = paper.Wallet is not null;
                foreach (var instrument in Definition.Instruments)
                {
                    try
                    {
                        var canonical = await Service.GetInstrumentAsync(instrument.InstrumentId, lifetime.Token);
                        instrumentNames[instrument.InstrumentId] = $"{canonical.Symbol} · {canonical.Name}";
                    }
                    catch (PlatformApiException) { /* The canonical ID remains visible and server validation reports unavailable instruments. */ }
                }
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception exception) { MetadataFailed = true; Error = FormatError(exception); }
        finally { Loading = false; }
    }

    private async Task ImportInstrument()
    {
        if (Disabled || Definition.Instruments.Count >= 50 || string.IsNullOrWhiteSpace(ImportSymbol)) return;
        Importing = true;
        Error = null;
        try
        {
            var instrument = await Service.ImportInstrumentAsync(new() { ProviderKey = ImportProvider, ProviderSymbol = ImportSymbol.Trim() }, lifetime.Token);
            instrumentNames[instrument.Id] = $"{instrument.Symbol} · {instrument.Name}";
            if (Definition.Instruments.Any(x => x.InstrumentId == instrument.Id))
            { Error = "Bu enstrüman zaten sepette."; return; }
            Definition.Instruments.Add(new() { InstrumentId = instrument.Id, MarketDataProviderKey = ImportProvider });
            InvalidateValidation();
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception exception) { Error = FormatError(exception); }
        finally { Importing = false; }
    }

    private async Task Validate()
    {
        if (Disabled) return;
        Validating = true;
        Error = null;
        try { Validation = await Service.ValidateAsync(Definition, lifetime.Token); }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception exception) { Error = FormatError(exception); }
        finally { Validating = false; }
    }

    private async Task Save()
    {
        if (Disabled) return;
        if (string.IsNullOrWhiteSpace(Name)) { Error = "Plan adı gerekli."; return; }
        if (!WeightValid) { Error = "Bileşen ağırlıklarının toplamı %100 olmalı."; return; }
        Saving = true;
        Error = null;
        try
        {
            Validation = await Service.ValidateAsync(Definition, lifetime.Token);
            if (!Validation.IsValid) return;
            var request = new SaveTrackingPlanRequest
            {
                Name = Name.Trim(), Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                Definition = TrackingEditorRules.Clone(Definition), ExpectedRevision = expectedRevision
            };
            var saved = loadedPlanId.HasValue
                ? await Service.UpdatePlanAsync(loadedPlanId.Value, request, lifetime.Token)
                : await Service.CreatePlanAsync(request, lifetime.Token);
            loadedPlanId = saved.Id;
            expectedRevision = saved.Revision;
            await Saved.InvokeAsync(saved);
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception exception) { Error = FormatError(exception); }
        finally { Saving = false; }
    }

    private long? expectedRevision;

    private static string FormatError(Exception exception) => exception switch
    {
        PlatformApiException api when api.StatusCode == HttpStatusCode.Conflict =>
            "Kayıt güncellenemedi: plan başka yerde değişmiş olabilir veya açık sanal pozisyon düzenlemeyi engelliyor. Değişikliklerin burada korunuyor. Güncel planı kontrol et; eski sürümün üzerine yazılmadı.",
        PlatformApiException api => string.IsNullOrWhiteSpace(api.RemoteMessage) ? "İstek tamamlanamadı. Bağlantını ve erişim yetkini kontrol et." : api.RemoteMessage,
        _ => "İstek tamamlanamadı. Bağlantını kontrol ederek yeniden dene."
    };

    private IndicatorDefinition? Metadata(TrackingIndicator binding) => Catalog.FirstOrDefault(x => x.Code == binding.IndicatorCode && x.Version == binding.IndicatorVersion);
    private string InstrumentLabel(Guid id) => instrumentNames.GetValueOrDefault(id, "Kayıtlı enstrüman");
    private bool CanAddRule() => TrackingEditorRules.Rules(Definition).Count() < 128;
    private void InvalidateValidation() => Validation = null;
    private void RemoveInstrument(TrackingInstrument instrument) { if (Disabled) return; Definition.Instruments.Remove(instrument); InvalidateValidation(); }
    private void RemoveIndicator(TrackingIndicator indicator) { if (Disabled || Definition.Indicators.Count <= 1 || TrackingEditorRules.References(Definition, indicator.BindingKey)) return; Definition.Indicators.Remove(indicator); InvalidateValidation(); }
    private void RemoveComponent(TrackingScoreComponent component) { if (Disabled || Definition.ScoreComponents.Count <= 1) return; Definition.ScoreComponents.Remove(component); InvalidateValidation(); }

    private void AddIndicator()
    {
        if (Disabled || Definition.Indicators.Count >= 32) return;
        var metadata = Catalog.FirstOrDefault(x => x.IsEnabled);
        if (metadata is null) return;
        var binding = new TrackingIndicator { BindingKey = UniqueKey("indicator", Definition.Indicators.Select(x => x.BindingKey)) };
        ApplyIndicator(binding, metadata);
        Definition.Indicators.Add(binding);
        InvalidateValidation();
    }

    private void ChangeIndicator(TrackingIndicator binding, ChangeEventArgs args)
    {
        if (Disabled) return;
        var metadata = Catalog.FirstOrDefault(x => $"{x.Code}:{x.Version}" == args.Value?.ToString() && x.IsEnabled);
        if (metadata is null) return;
        ApplyIndicator(binding, metadata);
        InvalidateValidation();
    }

    private static void ApplyIndicator(TrackingIndicator binding, IndicatorDefinition metadata)
    {
        binding.IndicatorCode = metadata.Code;
        binding.IndicatorVersion = metadata.Version;
        binding.Parameters = metadata.Parameters.ToDictionary(x => x.Key, x => x.Default);
        if (!metadata.SupportedSources.Contains(binding.Source)) binding.Source = metadata.SupportedSources.FirstOrDefault();
        if (!metadata.SupportedTimeframes.Contains(binding.Timeframe)) binding.Timeframe = metadata.SupportedTimeframes.FirstOrDefault();
    }

    private void RenameBinding(TrackingIndicator binding, ChangeEventArgs args)
    {
        if (Disabled) return;
        var key = args.Value?.ToString()?.Trim() ?? "";
        if (Definition.Indicators.Any(x => x != binding && x.BindingKey == key)) { Error = "Bağlantı adları benzersiz olmalı."; return; }
        TrackingEditorRules.RenameBinding(Definition, binding, key);
        InvalidateValidation();
    }

    private void AddComponent()
    {
        if (Disabled || Definition.ScoreComponents.Count >= 32 || !CanAddRule()) return;
        Definition.ScoreComponents.Add(new()
        {
            Key = UniqueKey("component", Definition.ScoreComponents.Select(x => x.Key)),
            WeightPercent = 0, Condition = TrackingEditorRules.EmptyComparison(), WhenTrueScore = 100
        });
        InvalidateValidation();
    }

    private void ChangeSizing(ChangeEventArgs args)
    {
        if (Disabled || !Enum.TryParse<TrackingSizingKind>(args.Value?.ToString(), out var sizing)) return;
        Definition.Execution.SizingKind = sizing;
        Definition.Execution.FixedQuoteAmount = sizing == TrackingSizingKind.FixedQuoteAmount ? 100 : null;
        Definition.Execution.AvailableBalancePercent = sizing == TrackingSizingKind.AvailableBalancePercent ? 10 : null;
        InvalidateValidation();
    }

    private void ChangeOrderType(ChangeEventArgs args)
    {
        if (Disabled || !Enum.TryParse<TrackingOrderType>(args.Value?.ToString(), out var orderType)) return;
        Definition.Execution.OrderType = orderType;
        if (orderType == TrackingOrderType.Market) Definition.Execution.LimitPrice = null;
        InvalidateValidation();
    }

    private static string UniqueKey(string prefix, IEnumerable<string> existing)
    {
        var keys = existing.ToHashSet(StringComparer.Ordinal);
        for (var number = 1; ; number++) if (!keys.Contains(prefix + number)) return prefix + number;
    }

    private static string ParameterLabel(string key) => key switch
    { "period" => "Periyot", "fastPeriod" => "Hızlı periyot", "slowPeriod" => "Yavaş periyot", "signalPeriod" => "Sinyal periyodu", _ => key };
    private async Task Cancel() { if (!Busy) await Cancelled.InvokeAsync(); }
    public void Dispose() { lifetime.Cancel(); lifetime.Dispose(); }
}
