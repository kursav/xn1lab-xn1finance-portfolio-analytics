using System.Globalization;
using System.Net;
using System.Text.Json;
using XN1Lab.Platform.Api.Exceptions;
using XN1Lab.Platform.Shared.Pagination;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Pages;

public partial class TrackingPlansPage
{
    private const int PageSize = 20;
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly JsonSerializerOptions SnapshotJson = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly (string Key, string Label, string Icon)[] Tabs =
    [
        ("overview", "Plan özeti", "sliders horizontal"), ("preview", "Son hesaplama", "calculator"),
        ("evaluations", "Hesaplama geçmişi", "history"), ("signals", "Al/Sat sinyalleri", "bolt"),
        ("paper", "Sanal portföy", "flask"), ("notifications", "Bildirimler", "bell outline"),
        ("versions", "Değişiklik geçmişi", "clone outline")
    ];

    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _listRequest;
    private CancellationTokenSource? _selectionRequest;
    private CancellationTokenSource? _panelRequest;
    private readonly Dictionary<Guid, string> _instrumentNames = [];
    private bool _disposed;
    private TrackingCapabilities? Capabilities;
    private TrackingPlan? Selected;
    private TrackingPlan? EditorPlan;
    private TrackingPlanDefinition? Definition => Selected?.CurrentVersion?.Definition;
    private Guid EditorKey = Guid.NewGuid();
    private bool EditorOpen;
    private bool ListLoading;
    private bool ListHasLoaded;
    private bool DetailLoading;
    private bool PanelLoading;
    private bool ActionBusy;
    private bool PreviewBusy;
    private bool Busy => ActionBusy || PreviewBusy || DetailLoading;
    private string? SearchText;
    private string StatusFilter = string.Empty;
    private bool HasFilters => !string.IsNullOrWhiteSpace(SearchText) || !string.IsNullOrWhiteSpace(StatusFilter);
    private string? PageError;
    private string? DetailError;
    private string? PanelError;
    private string? SuccessMessage;
    private string ActiveTab = "overview";
    private TrackingPlanStatus? PendingStatus;
    private TrackingPlanValidationResult? ActivationValidation;
    private IReadOnlyList<FinancePreviewResult>? Previews;
    private FinanceEvaluation? ExpandedEvaluation;
    private FinancePaperPortfolio? Paper;
    private TrackingPlanVersion? InspectedVersion;
    private bool UnreadOnly;
    private PaginatedList<TrackingPlan> PlanPage = new();
    private PaginatedList<FinanceEvaluation> EvaluationPage = new();
    private PaginatedList<FinanceSignal> SignalPage = new();
    private PaginatedList<FinancePaperTrade> TradePage = new();
    private PaginatedList<FinanceNotification> NotificationPage = new();
    private PaginatedList<TrackingPlanVersion> VersionPage = new();
    private int ListPageNumber = 1;
    private int EvaluationPageNumber = 1;
    private int SignalPageNumber = 1;
    private int TradePageNumber = 1;
    private int NotificationPageNumber = 1;
    private int VersionPageNumber = 1;
    private string VersionJson => JsonSerializer.Serialize(InspectedVersion, SnapshotJson);

    protected override async Task OnInitializedAsync()
    {
        if (Permissions.CanRead) await ReloadWorkspaceAsync();
    }

    private async Task ReloadWorkspaceAsync()
    {
        if (!Permissions.CanRead || _disposed) return;
        PageError = null;
        Capabilities = null;
        try { Capabilities = await TrackingService.GetCapabilitiesAsync(_lifetime.Token); }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { return; }
        catch (Exception exception) { PageError = ErrorText(exception); }
        await LoadPlansAsync(clearError: false);
    }

    private async Task LoadPlansAsync(bool clearError = true)
    {
        if (!Permissions.CanRead || _disposed) return;
        CancelAndDispose(ref _listRequest);
        var request = _listRequest = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        var token = request.Token;
        ListLoading = true;
        if (clearError) PageError = null;
        try
        {
            var status = Enum.TryParse<TrackingPlanStatus>(StatusFilter, out var filter) ? filter : (TrackingPlanStatus?)null;
            var result = await TrackingService.GetPlansAsync(status, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(), ListPageNumber, PageSize, token);
            if (!token.IsCancellationRequested) { PlanPage = result; ListHasLoaded = true; }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) { if (!token.IsCancellationRequested) PageError = ErrorText(exception); }
        finally { if (!token.IsCancellationRequested && !_disposed) ListLoading = false; }
    }

    private async Task SearchAsync() { ListPageNumber = 1; await LoadPlansAsync(); }
    private async Task ChangeListPageAsync(int page) { ListPageNumber = Math.Max(1, page); await LoadPlansAsync(); }

    private async Task SelectPlanAsync(Guid planId)
    {
        if (!Permissions.CanRead || _disposed || ActionBusy || PreviewBusy || EditorOpen) return;
        CancelAndDispose(ref _selectionRequest);
        CancelAndDispose(ref _panelRequest);
        var request = _selectionRequest = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        var token = request.Token;
        DetailLoading = true;
        PanelLoading = false;
        DetailError = PanelError = SuccessMessage = null;
        Selected = null;
        PendingStatus = null;
        ActivationValidation = null;
        ResetPlanData();
        try
        {
            var plan = await TrackingService.GetPlanAsync(planId, token);
            if (token.IsCancellationRequested) return;
            Selected = plan;
            await LoadInstrumentNamesAsync(plan, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) { if (!token.IsCancellationRequested) PageError = ErrorText(exception); }
        finally { if (!token.IsCancellationRequested && !_disposed) DetailLoading = false; }
    }

    private async Task LoadInstrumentNamesAsync(TrackingPlan plan, CancellationToken token)
    {
        if (!Permissions.CanRead || !Permissions.CanReadMarketData || plan.CurrentVersion is null) return;
        foreach (var instrument in plan.CurrentVersion.Definition.Instruments)
        {
            if (_instrumentNames.ContainsKey(instrument.InstrumentId)) continue;
            try
            {
                var result = await TrackingService.GetInstrumentAsync(instrument.InstrumentId, token);
                if (token.IsCancellationRequested) return;
                if (!string.IsNullOrWhiteSpace(result.Symbol)) _instrumentNames[instrument.InstrumentId] = result.Symbol;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
            catch (Exception) { /* Instrument labels are optional; finance history remains usable by canonical ID. */ }
        }
    }

    private void ResetPlanData()
    {
        ActiveTab = "overview";
        Previews = null;
        ExpandedEvaluation = null;
        Paper = null;
        InspectedVersion = null;
        EvaluationPage = new(); SignalPage = new(); TradePage = new(); NotificationPage = new(); VersionPage = new();
        EvaluationPageNumber = SignalPageNumber = TradePageNumber = NotificationPageNumber = VersionPageNumber = 1;
    }

    private void CreatePlan()
    {
        if (!Permissions.CanRead || !Permissions.CanWrite || Busy) return;
        EditorPlan = null;
        EditorKey = Guid.NewGuid();
        EditorOpen = true;
        PendingStatus = null;
    }

    private void EditPlan()
    {
        if (!Permissions.CanRead || !Permissions.CanWrite || Busy || Selected is null || Selected.Status == TrackingPlanStatus.Archived) return;
        EditorPlan = Selected;
        EditorKey = Guid.NewGuid();
        EditorOpen = true;
        PendingStatus = null;
    }

    private void CloseEditor() { EditorOpen = false; EditorPlan = null; }

    private async Task OnSavedAsync(TrackingPlan plan)
    {
        if (!Permissions.CanRead || _disposed) return;
        CloseEditor();
        await LoadPlansAsync();
        await SelectPlanAsync(plan.Id);
        if (Selected?.Id == plan.Id) SuccessMessage = $"Plan sürüm {plan.CurrentVersionNumber} olarak kaydedildi. Etkinleştirme ayrı bir işlemdir.";
    }

    private async Task ReloadSelectedAsync()
    {
        if (Selected is null || Busy || !Permissions.CanRead) return;
        var tab = ActiveTab;
        var id = Selected.Id;
        await SelectPlanAsync(id);
        if (Selected?.Id == id) await SwitchTabAsync(tab);
        await LoadPlansAsync();
    }

    private async Task PrepareActivationAsync()
    {
        if (!CanChangeStatus || Definition is null || Definition.Execution.Mode == TrackingExecutionMode.Live || Capabilities?.SchedulerAvailable != true) return;
        ActionBusy = true;
        DetailError = SuccessMessage = null;
        ActivationValidation = null;
        PendingStatus = TrackingPlanStatus.Active;
        var token = SelectionToken;
        try { ActivationValidation = await TrackingService.ValidateAsync(Definition, token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) { DetailError = ErrorText(exception); }
        finally { ActionBusy = false; }
    }

    private bool CanChangeStatus => Permissions.CanRead && Permissions.CanWrite && Selected is not null && !Busy && !_disposed;
    private CancellationToken SelectionToken => _selectionRequest?.Token ?? _lifetime.Token;
    private void PrepareArchive() { if (CanChangeStatus) { PendingStatus = TrackingPlanStatus.Archived; ActivationValidation = null; } }
    private void CancelStatus() { PendingStatus = null; ActivationValidation = null; }
    private Task ConfirmStatusAsync() => PendingStatus.HasValue && (PendingStatus != TrackingPlanStatus.Active || ActivationValidation?.CanActivate == true)
        ? ChangeStatusAsync(PendingStatus.Value) : Task.CompletedTask;

    private async Task ChangeStatusAsync(TrackingPlanStatus status)
    {
        if (!CanChangeStatus || Selected is null) return;
        if (status == TrackingPlanStatus.Active && (Capabilities?.SchedulerAvailable != true || Definition?.Execution.Mode == TrackingExecutionMode.Live || ActivationValidation?.CanActivate != true)) return;
        var plan = Selected;
        var token = SelectionToken;
        ActionBusy = true;
        DetailError = SuccessMessage = null;
        try
        {
            var changed = await TrackingService.ChangeStatusAsync(plan.Id, new ChangeTrackingPlanStatusRequest { Status = status, ExpectedRevision = plan.Revision }, token);
            if (token.IsCancellationRequested || Selected?.Id != plan.Id) return;
            Selected = changed;
            PendingStatus = null;
            ActivationValidation = null;
            SuccessMessage = status switch
            {
                TrackingPlanStatus.Active => "Otomatik takip başladı. Sunucu planın takvimine göre tarama yapacak.",
                TrackingPlanStatus.Paused => "Otomatik tarama durdu. Yeni girişler durdu; açık sanal pozisyonların koruma kontrolleri devam eder.",
                TrackingPlanStatus.Archived => "Plan arşivlendi. Geçmiş kayıtları görüntülemeye devam edebilirsin.",
                _ => "Plan durumu güncellendi."
            };
            await LoadPlansAsync();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) { if (!token.IsCancellationRequested) { DetailError = ErrorText(exception); PendingStatus = null; ActivationValidation = null; } }
        finally { if (!_disposed) ActionBusy = false; }
    }

    private async Task PreviewAsync()
    {
        if (!Permissions.CanRead || Selected is null || Busy || Capabilities?.EvaluationAvailable != true) return;
        CancelAndDispose(ref _panelRequest);
        PanelLoading = false;
        var planId = Selected.Id;
        var token = SelectionToken;
        PreviewBusy = true;
        ActiveTab = "preview";
        PanelError = null;
        Previews = null;
        try
        {
            var result = await TrackingService.PreviewAsync(planId, token);
            if (token.IsCancellationRequested || Selected?.Id != planId) return;
            Previews = result;
            foreach (var item in result.Where(item => item.Quote is not null)) _instrumentNames[item.InstrumentId] = item.Quote!.ProviderSymbol;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) { if (!token.IsCancellationRequested) PanelError = ErrorText(exception); }
        finally { if (!_disposed) PreviewBusy = false; }
    }

    private async Task SwitchTabAsync(string tab)
    {
        if (!Permissions.CanRead || Selected is null || Busy || !Tabs.Any(item => item.Key == tab)) return;
        ActiveTab = tab;
        await LoadPanelAsync();
    }

    private Task RefreshPanelAsync() => ActiveTab == "preview" ? PreviewAsync() : LoadPanelAsync();

    private async Task LoadPanelAsync()
    {
        if (!Permissions.CanRead || Selected is null || _disposed) return;
        CancelAndDispose(ref _panelRequest);
        var request = _panelRequest = CancellationTokenSource.CreateLinkedTokenSource(SelectionToken);
        var token = request.Token;
        var planId = Selected.Id;
        var tab = ActiveTab;
        PanelError = null;
        PanelLoading = tab is not ("overview" or "preview");
        try
        {
            switch (tab)
            {
                case "evaluations":
                    var evaluations = await TrackingService.GetEvaluationsAsync(planId, EvaluationPageNumber, PageSize, token);
                    if (!token.IsCancellationRequested) { EvaluationPage = evaluations; ExpandedEvaluation = null; }
                    break;
                case "signals":
                    var signals = await TrackingService.GetSignalsAsync(planId, SignalPageNumber, PageSize, token);
                    if (!token.IsCancellationRequested) SignalPage = signals;
                    break;
                case "paper":
                    var paperTask = TrackingService.GetPaperAsync(planId, token);
                    var tradesTask = TrackingService.GetPaperTradesAsync(planId, TradePageNumber, PageSize, token);
                    await Task.WhenAll(paperTask, tradesTask);
                    if (!token.IsCancellationRequested) { Paper = await paperTask; TradePage = await tradesTask; }
                    break;
                case "notifications":
                    var notifications = await TrackingService.GetNotificationsAsync(planId, UnreadOnly, NotificationPageNumber, PageSize, token);
                    if (!token.IsCancellationRequested) NotificationPage = notifications;
                    break;
                case "versions":
                    var versions = await TrackingService.GetVersionsAsync(planId, VersionPageNumber, PageSize, token);
                    if (!token.IsCancellationRequested) VersionPage = versions;
                    break;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) { if (!token.IsCancellationRequested) PanelError = ErrorText(exception); }
        finally { if (!token.IsCancellationRequested && !_disposed) PanelLoading = false; }
    }

    private void ToggleEvaluation(FinanceEvaluation evaluation) => ExpandedEvaluation = ExpandedEvaluation?.Id == evaluation.Id ? null : evaluation;
    private async Task ChangeEvaluationPageAsync(int page) { EvaluationPageNumber = Math.Max(1, page); await LoadPanelAsync(); }
    private async Task ChangeSignalPageAsync(int page) { SignalPageNumber = Math.Max(1, page); await LoadPanelAsync(); }
    private async Task ChangeTradePageAsync(int page) { TradePageNumber = Math.Max(1, page); await LoadPanelAsync(); }
    private async Task ChangeNotificationPageAsync(int page) { NotificationPageNumber = Math.Max(1, page); await LoadPanelAsync(); }
    private async Task ChangeVersionPageAsync(int page) { VersionPageNumber = Math.Max(1, page); await LoadPanelAsync(); }
    private async Task ChangeUnreadAsync() { NotificationPageNumber = 1; await LoadPanelAsync(); }

    private async Task MarkReadAsync(FinanceNotification notification)
    {
        if (!Permissions.CanRead || !Permissions.CanWrite || Busy || Selected?.Id != notification.PlanId || notification.ReadAtUtc.HasValue) return;
        var token = SelectionToken;
        ActionBusy = true;
        PanelError = null;
        try
        {
            await TrackingService.MarkNotificationReadAsync(notification.Id, token);
            if (!token.IsCancellationRequested)
            {
                if (UnreadOnly && NotificationPage.Items.Count == 1 && NotificationPageNumber > 1) NotificationPageNumber--;
                await LoadPanelAsync();
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) { if (!token.IsCancellationRequested) PanelError = ErrorText(exception); }
        finally { if (!_disposed) ActionBusy = false; }
    }

    private async Task InspectVersionAsync(int versionNumber)
    {
        if (!Permissions.CanRead || Selected is null || Busy) return;
        var token = SelectionToken;
        var planId = Selected.Id;
        ActionBusy = true;
        PanelError = null;
        try
        {
            var version = await TrackingService.GetVersionAsync(planId, versionNumber, token);
            if (!token.IsCancellationRequested && Selected?.Id == planId) InspectedVersion = version;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) { if (!token.IsCancellationRequested) PanelError = ErrorText(exception); }
        finally { if (!_disposed) ActionBusy = false; }
    }

    private string InstrumentName(Guid id) => _instrumentNames.TryGetValue(id, out var name) ? name : $"Varlık {id.ToString("N")[..8]}";
    private static string StatusLabel(TrackingPlanStatus status) => status switch { TrackingPlanStatus.Active => "Takip açık", TrackingPlanStatus.Paused => "Takip durdu", TrackingPlanStatus.Archived => "Arşiv", _ => "Taslak" };
    private static string ModeLabel(TrackingExecutionMode? mode) => mode switch { TrackingExecutionMode.Paper => "Sanal işlem", TrackingExecutionMode.Live => "Gerçek işlem (desteklenmiyor)", TrackingExecutionMode.Observe => "Yalnızca izle", _ => "—" };
    private static string ActionLabel(FinanceDecisionAction action) => action switch { FinanceDecisionAction.Buy => "Al", FinanceDecisionAction.Sell => "Sat", _ => "Bekle" };
    private static string EvaluationKindLabel(FinanceEvaluationKind kind) => kind switch { FinanceEvaluationKind.Decision => "Karar", FinanceEvaluationKind.Protection => "Koruma", _ => "Veri eksik" };
    private static string TimeframeLabel(TrackingTimeframe timeframe) => timeframe switch { TrackingTimeframe.Minute5 => "5 dakika", TrackingTimeframe.Minute15 => "15 dakika", TrackingTimeframe.Minute30 => "30 dakika", TrackingTimeframe.Hour1 => "1 saat", TrackingTimeframe.Hour2 => "2 saat", TrackingTimeframe.Hour4 => "4 saat", TrackingTimeframe.Day1 => "1 gün", TrackingTimeframe.Week1 => "1 hafta", _ => timeframe.ToString() };
    private static string Number(decimal? value) => value?.ToString("N2", Turkish) ?? "—";
    private static string Price(decimal? value) => value?.ToString("#,##0.########", Turkish) ?? "—";
    private static string Quantity(decimal value) => value.ToString("0.########", Turkish);
    private static string Money(decimal value, string currency) => $"{Number(value)} {currency}";
    private static string Percent(decimal? value) => value.HasValue ? $"%{Number(value)}" : "Belirlenmedi";
    private static string Utc(DateTime value) => value == default ? "—" : value.ToString("dd.MM.yyyy HH:mm", Turkish);
    private static string ErrorText(Exception exception) => exception switch
    {
        PlatformApiException api when api.StatusCode == HttpStatusCode.Conflict => "Plan başka bir işlem tarafından değiştirilmiş veya mevcut durum bu işleme uygun değil. Yenile düğmesiyle güncel sürümü yükle; değişiklik otomatik olarak tekrarlanmadı.",
        PlatformApiException api when api.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => "Bu işlem için oturumun veya hesap yetkin yeterli değil.",
        PlatformApiException api when api.StatusCode == HttpStatusCode.NotFound => "Plan veya takip API'si bulunamadı. Sunucuda takip modülünün yayımlandığını kontrol et.",
        PlatformApiException api when (int)api.StatusCode >= 500 => "Takip servisine şu anda ulaşılamıyor. Kayıtların yerine örnek veri gösterilmiyor; yeniden deneyebilirsin.",
        PlatformApiException api => string.IsNullOrWhiteSpace(api.RemoteMessage) ? "Sunucu işlemi kabul etmedi. Planı ve giriş değerlerini kontrol et." : api.RemoteMessage,
        OperationCanceledException => "İstek zaman aşımına uğradı. Yeniden deneyebilirsin.",
        HttpRequestException => "Sunucu bağlantısı kurulamadı. Bağlantını kontrol edip yeniden dene.",
        _ => "Veriler yüklenirken bir hata oluştu. Yeniden deneyebilirsin."
    };

    private static void CancelAndDispose(ref CancellationTokenSource? source)
    {
        source?.Cancel();
        source?.Dispose();
        source = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        CancelAndDispose(ref _listRequest);
        CancelAndDispose(ref _panelRequest);
        CancelAndDispose(ref _selectionRequest);
        _lifetime.Dispose();
    }
}
