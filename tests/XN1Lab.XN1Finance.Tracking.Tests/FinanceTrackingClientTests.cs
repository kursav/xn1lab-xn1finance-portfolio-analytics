using System.Net;
using System.Text.Json;
using XN1Lab.Platform.Api.Abstractions;
using XN1Lab.Platform.Api.Exceptions;
using XN1Lab.Platform.Api.Models;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Components;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Services;
using Xunit;

namespace XN1Lab.XN1Finance.Tracking.Tests;

public sealed class FinanceTrackingClientTests
{
    private static readonly Guid PlanId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid NotificationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string Root = "api/xn1finance/tracking-plans";

    [Theory]
    [InlineData("capabilities", "GET", "capabilities", "{}")]
    [InlineData("strategy-results", "GET", "strategy-results", "{\"generatedAtUtc\":\"2026-09-13T12:00:00Z\",\"fromUtc\":\"2026-09-06T12:00:00Z\",\"toUtc\":\"2026-09-13T12:00:00Z\",\"plans\":[]}")]
    [InlineData("indicators", "GET", "indicators", "[]")]
    [InlineData("providers", "GET", "providers", "[]")]
    [InlineData("validate", "POST", "validate", "{\"errors\":[],\"activationBlockers\":[]}")]
    [InlineData("plans", "GET", "", "{\"items\":[],\"totalItems\":0}")]
    [InlineData("plan", "GET", "{plan}", "{}")]
    [InlineData("versions", "GET", "{plan}/versions", "{\"items\":[]}")]
    [InlineData("version", "GET", "{plan}/versions/3", "{}")]
    [InlineData("create", "POST", "", "{}")]
    [InlineData("update", "PUT", "{plan}", "{}")]
    [InlineData("status", "PATCH", "{plan}/status", "{}")]
    [InlineData("import", "POST", "instruments/import", "{}")]
    [InlineData("preview", "POST", "{plan}/preview", "[]")]
    [InlineData("evaluations", "GET", "{plan}/evaluations", "{\"items\":[]}")]
    [InlineData("signals", "GET", "{plan}/signals", "{\"items\":[]}")]
    [InlineData("paper", "GET", "{plan}/paper", "{}")]
    [InlineData("trades", "GET", "{plan}/paper/trades", "{\"items\":[]}")]
    [InlineData("notifications", "GET", "notifications", "{\"items\":[]}")]
    [InlineData("read", "PATCH", "notifications/{notification}/read", "")]
    public async Task Every_tracking_operation_uses_exact_route_method_and_authenticated_session_scope(
        string operation, string method, string suffix, string json)
    {
        using var cancellation = new CancellationTokenSource();
        var api = new RecordingApiClient
        {
            Response = new(operation == "read" ? HttpStatusCode.NoContent : HttpStatusCode.OK, json)
        };
        var service = new ApiFinanceTrackingService(api);

        await InvokeAsync(service, operation, cancellation.Token);

        Assert.Equal(method, api.Method?.Method);
        var expectedSuffix = suffix.Replace("{plan}", PlanId.ToString("D"))
            .Replace("{notification}", NotificationId.ToString("D"));
        Assert.Equal(string.IsNullOrEmpty(expectedSuffix) ? Root : $"{Root}/{expectedSuffix}", api.Path);
        Assert.Null(api.OwnerAccountId);
        Assert.Equal(cancellation.Token, api.CancellationToken);
        Assert.Equal(1, api.Calls);
    }

    [Fact]
    public async Task Strategy_results_use_UTC_period_and_indexed_plan_selection_with_no_account_override()
    {
        var api = new RecordingApiClient { Response = new(HttpStatusCode.OK,
            """{"generatedAtUtc":"2026-09-13T12:00:00Z","fromUtc":"2026-09-06T12:00:00Z","toUtc":"2026-09-13T12:00:00Z","plans":[]}""") };
        var from = new DateTime(2026, 9, 6, 12, 0, 0, DateTimeKind.Utc);
        var to = from.AddDays(7);
        using var cancellation = new CancellationTokenSource();
        var results = await new ApiFinanceTrackingService(api).GetStrategyResultsAsync(from, to, [PlanId, NotificationId], cancellation.Token);

        Assert.Equal($"{Root}/strategy-results", api.Path);
        Assert.Equal(HttpMethod.Get, api.Method);
        Assert.Equal("2026-09-06T12:00:00.0000000Z", api.Query!["fromUtc"]);
        Assert.Equal("2026-09-13T12:00:00.0000000Z", api.Query["toUtc"]);
        Assert.Equal(PlanId.ToString("D"), api.Query["planIds[0]"]);
        Assert.Equal(NotificationId.ToString("D"), api.Query["planIds[1]"]);
        Assert.Null(api.OwnerAccountId);
        Assert.Equal(cancellation.Token, api.CancellationToken);
        Assert.Equal(from, results.FromUtc);
        Assert.Equal(to, results.ToUtc);
        Assert.Empty(results.Plans);
    }

    [Fact]
    public async Task Strategy_results_keep_independent_version_rows_and_server_aggregates()
    {
        var api = new RecordingApiClient { Response = new(HttpStatusCode.OK,
            """{"generatedAtUtc":"2026-09-13T12:00:00Z","fromUtc":"2026-09-06T12:00:00Z","toUtc":"2026-09-13T12:00:00Z","plans":[{"planId":"11111111-1111-1111-1111-111111111111","versionNumber":1,"mode":"Paper","direction":[{"horizonMinutes":5,"eligible":10,"measured":8,"successes":5,"neutral":1,"pending":1,"missing":1,"successRatePercent":62.5}],"paper":{"closedEpisodeCount":4,"wins":1,"netPnl":-3.75,"winRatePercent":25},"indicatorDefinitions":[{"bindingKey":"rsi5","indicatorCode":"RSI","timeframe":"Minute5","parameters":{"period":14}}]},{"planId":"11111111-1111-1111-1111-111111111111","versionNumber":2,"mode":"Paper"}]}""") };
        var result = await new ApiFinanceTrackingService(api).GetStrategyResultsAsync();
        Assert.Equal(2, result.Plans.Count);
        Assert.Equal(result.Plans[0].PlanId, result.Plans[1].PlanId);
        Assert.Equal(1, result.Plans[0].VersionNumber);
        Assert.Equal(2, result.Plans[1].VersionNumber);
        var measured = Assert.Single(result.Plans[0].Direction);
        Assert.Equal(8, measured.Measured);
        Assert.Equal(62.5m, measured.SuccessRatePercent);
        Assert.Equal(4, result.Plans[0].Paper.ClosedEpisodeCount);
        Assert.Equal(-3.75m, result.Plans[0].Paper.NetPnl);
        Assert.Null(result.Plans[1].Paper.WinRatePercent);
        Assert.Equal(14, Assert.Single(result.Plans[0].IndicatorDefinitions).Parameters["period"]);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"generatedAtUtc\":\"2026-09-13T12:00:00Z\",\"fromUtc\":\"2026-09-06T12:00:00Z\",\"toUtc\":\"2026-09-13T12:00:00Z\",\"plans\":null}")]
    public async Task Incomplete_strategy_response_is_not_a_successful_empty_result(string json)
    {
        var api = new RecordingApiClient { Response = new(HttpStatusCode.OK, json) };
        await Assert.ThrowsAsync<FinanceTrackingApiContractException>(() => new ApiFinanceTrackingService(api).GetStrategyResultsAsync());
    }

    [Fact]
    public async Task Invalid_strategy_period_or_selection_fails_before_HTTP()
    {
        var api = new RecordingApiClient();
        var service = new ApiFinanceTrackingService(api);
        var from = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetStrategyResultsAsync(from, from));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetStrategyResultsAsync(from, from.AddDays(32)));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetStrategyResultsAsync(planIds: [Guid.Empty]));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetStrategyResultsAsync(planIds: Enumerable.Range(0, 21).Select(_ => Guid.NewGuid()).ToArray()));
        Assert.Equal(0, api.Calls);
    }

    [Fact]
    public async Task Canonical_instrument_lookup_preserves_existing_portfolio_route()
    {
        var api = new RecordingApiClient { Response = new(HttpStatusCode.OK, "{\"symbol\":\"BTCUSDT\",\"currency\":\"USDT\",\"instrumentType\":\"Crypto\"}") };
        var instrument = await new ApiFinanceTrackingService(api).GetInstrumentAsync(PlanId);
        Assert.Equal($"api/xn1finance/portfolio-analytics/instruments/{PlanId:D}", api.Path);
        Assert.Equal(HttpMethod.Get, api.Method);
        Assert.Equal("BTCUSDT", instrument.Symbol);
        Assert.Equal("USDT", instrument.Currency);
        Assert.Null(api.OwnerAccountId);
    }

    [Fact]
    public async Task Plan_search_and_paging_use_backend_query_names_and_keep_total_count()
    {
        var api = new RecordingApiClient { Response = new(HttpStatusCode.OK,
            "{\"items\":[],\"totalItems\":73,\"pageIndex\":2,\"pageSize\":20,\"totalPages\":4}") };
        var page = await new ApiFinanceTrackingService(api).GetPlansAsync(TrackingPlanStatus.Paused, "  BTC / USDT  ", 2, 20);
        Assert.Equal("Paused", api.Query!["status"]);
        Assert.Equal("BTC / USDT", api.Query["search"]);
        Assert.False(api.Query.ContainsKey("searchText"));
        Assert.Equal("2", api.Query["pageNumber"]);
        Assert.Equal("20", api.Query["pageSize"]);
        Assert.Equal(73, page.TotalItems);
        Assert.True(page.HasNextPage);
    }

    [Theory]
    [InlineData("versions")]
    [InlineData("evaluations")]
    [InlineData("signals")]
    [InlineData("trades")]
    [InlineData("notifications")]
    public async Task History_operations_preserve_page_number_and_size(string operation)
    {
        var api = new RecordingApiClient { Response = new(HttpStatusCode.OK, "{\"items\":[]}") };
        await InvokeAsync(new ApiFinanceTrackingService(api), operation, default);
        Assert.Equal("2", api.Query!["pageNumber"]);
        Assert.Equal("20", api.Query["pageSize"]);
        if (operation == "notifications")
        {
            Assert.Equal(PlanId.ToString("D"), api.Query["planId"]);
            Assert.Equal("true", api.Query["unreadOnly"]);
        }
    }

    [Fact]
    public async Task Updates_send_loaded_revision_and_do_not_assign_account_ownership()
    {
        var api = new RecordingApiClient();
        var service = new ApiFinanceTrackingService(api);
        var request = new SaveTrackingPlanRequest { Name = "Editable draft", ExpectedRevision = 12 };
        await service.UpdatePlanAsync(PlanId, request);
        Assert.Same(request, api.Body);
        var requestJson = JsonSerializer.Serialize(api.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Contains("\"expectedRevision\":12", requestJson);
        Assert.DoesNotContain("ownerAccountId", requestJson);

        var statusRequest = new ChangeTrackingPlanStatusRequest { Status = TrackingPlanStatus.Paused, ExpectedRevision = 13 };
        await service.ChangeStatusAsync(PlanId, statusRequest);
        Assert.Same(statusRequest, api.Body);
        Assert.Contains("\"status\":\"Paused\"", JsonSerializer.Serialize(api.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    [Fact]
    public async Task Preview_and_mark_read_send_no_synthetic_body()
    {
        var api = new RecordingApiClient { Response = new(HttpStatusCode.OK, "[]") };
        var service = new ApiFinanceTrackingService(api);
        await service.PreviewAsync(PlanId);
        Assert.Null(api.Body);
        api.Response = new(HttpStatusCode.NoContent, "");
        await service.MarkNotificationReadAsync(NotificationId);
        Assert.Null(api.Body);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(503)]
    public async Task API_errors_propagate_and_are_never_replaced_with_empty_success(int status)
    {
        var api = new RecordingApiClient { Response = new((HttpStatusCode)status, "{\"message\":\"Request rejected\"}") };
        var exception = await Assert.ThrowsAsync<PlatformApiException>(() => new ApiFinanceTrackingService(api).GetPlansAsync());
        Assert.Equal((HttpStatusCode)status, exception.StatusCode);
    }

    [Fact]
    public async Task Platform_exception_instance_is_preserved_for_localized_error_handling()
    {
        var failure = new PlatformApiException(HttpStatusCode.Conflict, "{\"type\":\"exceptions.xn1finance.plan_revision_conflict\"}");
        var api = new RecordingApiClient { Failure = failure };
        var actual = await Assert.ThrowsAsync<PlatformApiException>(() => new ApiFinanceTrackingService(api).GetPlansAsync());
        Assert.Same(failure, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("<html>Gateway unavailable</html>")]
    [InlineData("[]")]
    public async Task Null_empty_and_malformed_required_responses_are_visible_failures(string content)
    {
        var api = new RecordingApiClient { Response = new(HttpStatusCode.OK, content) };
        await Assert.ThrowsAsync<FinanceTrackingApiContractException>(() => new ApiFinanceTrackingService(api).GetPlanAsync(PlanId));
    }

    [Fact]
    public async Task Mark_read_requires_no_content_response()
    {
        var api = new RecordingApiClient { Response = new(HttpStatusCode.OK, "{}") };
        await Assert.ThrowsAsync<FinanceTrackingApiContractException>(() => new ApiFinanceTrackingService(api).MarkNotificationReadAsync(NotificationId));
    }

    [Fact]
    public async Task Cancellation_is_preserved()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var api = new RecordingApiClient();
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ApiFinanceTrackingService(api).GetPlansAsync(cancellationToken: cancellation.Token));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    [Fact]
    public async Task Missing_revision_and_invalid_paging_fail_before_HTTP()
    {
        var api = new RecordingApiClient();
        var service = new ApiFinanceTrackingService(api);
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePlanAsync(PlanId, new SaveTrackingPlanRequest()));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.ChangeStatusAsync(PlanId, new ChangeTrackingPlanStatusRequest()));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetPlansAsync(pageSize: 101));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetPlansAsync(pageNumber: 0));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetPlanAsync(Guid.Empty));
        Assert.Equal(0, api.Calls);
    }

    [Fact]
    public void Backend_example_shape_preserves_nested_rules_decimals_and_string_enums_on_round_trip()
    {
        // Contract fixture follows CoreServices docs/examples/xn1finance-tracking-plan.json.
        const string json = """
        {
          "name": "RSI and daily EMA",
          "definition": {
            "schemaVersion": 1,
            "instruments": [{"instrumentId":"11111111-1111-1111-1111-111111111111","marketDataProviderKey":"binance-spot"}],
            "indicators": [
              {"bindingKey":"rsi2h","indicatorCode":"RSI","indicatorVersion":1,"timeframe":"Hour2","source":"Close","parameters":{"period":14}},
              {"bindingKey":"emaDaily","indicatorCode":"EMA","indicatorVersion":1,"timeframe":"Day1","source":"Close","parameters":{"period":50}}
            ],
            "scoreComponents": [{"key":"oversold","weightPercent":100,"condition":{"kind":"Comparison","comparison":"LessThan","left":{"kind":"Indicator","indicatorBindingKey":"rsi2h","outputKey":"value"},"right":{"kind":"Constant","constant":30}},"whenTrueScore":100,"whenFalseScore":-50}],
            "entryRule":{"kind":"All","children":[{"kind":"Comparison","comparison":"CrossesAbove","left":{"kind":"TotalScore"},"right":{"kind":"Constant","constant":75}},{"kind":"Comparison","comparison":"GreaterThan","left":{"kind":"Price","timeframe":"Day1","priceField":"Close"},"right":{"kind":"Indicator","indicatorBindingKey":"emaDaily","outputKey":"value"}}]},
            "exitRule":{"kind":"Not","children":[{"kind":"Comparison","comparison":"GreaterThan","left":{"kind":"TotalScore"},"right":{"kind":"Constant","constant":25}}]},
            "schedule":{"decisionTimeframe":"Hour2","pollIntervalSeconds":60,"signalTtlSeconds":300,"cooldownSeconds":7200,"closedCandlesOnly":true},
            "execution":{"mode":"Paper","quoteCurrency":"USDT","paperInitialBalance":10000.25,"sizingKind":"AvailableBalancePercent","availableBalancePercent":10,"sellPositionPercent":100,"orderType":"Market","stopLossPercent":3,"takeProfitPercent":6,"maxOpenPositions":2,"maxPlanExposurePercent":20,"paperFeeBps":10,"paperSlippageBps":5},
            "notificationsEnabled":true
          }
        }
        """;
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var request = JsonSerializer.Deserialize<SaveTrackingPlanRequest>(json, options)!;
        Assert.Equal(TrackingExecutionMode.Paper, request.Definition.Execution.Mode);
        Assert.Equal(TrackingTimeframe.Day1, request.Definition.Indicators[1].Timeframe);
        Assert.Equal(10000.25m, request.Definition.Execution.PaperInitialBalance);
        Assert.Equal(TrackingComparison.CrossesAbove, request.Definition.EntryRule!.Children[0].Comparison);
        Assert.Equal(TrackingRuleKind.Not, request.Definition.ExitRule!.Kind);
        Assert.Equal(TrackingValueKind.TotalScore, request.Definition.EntryRule.Children[0].Left!.Kind);
        Assert.Equal(14, request.Definition.Indicators[0].Parameters["period"]);
        var encoded = JsonSerializer.Serialize(request, options);
        var decoded = JsonSerializer.Deserialize<SaveTrackingPlanRequest>(encoded, options)!;
        Assert.Equal(request.Definition.ScoreComponents[0].WhenFalseScore, decoded.Definition.ScoreComponents[0].WhenFalseScore);
        Assert.Equal(request.Definition.EntryRule.Children.Count, decoded.Definition.EntryRule!.Children.Count);
        Assert.Contains("\"timeframe\":\"Day1\"", encoded);
        Assert.Contains("\"mode\":\"Paper\"", encoded);
        Assert.Null(decoded.ExpectedRevision);
        Assert.DoesNotContain("ownerAccountId", encoded);
    }

    [Fact]
    public async Task Paper_protection_configuration_survives_API_load_clone_and_save()
    {
        var api = new RecordingApiClient { Response = new(HttpStatusCode.OK,
            """{"currentVersion":{"definition":{"execution":{"mode":"Paper","maxHoldingMinutes":30,"trailingStopPercent":0.25,"trailingStopActivationPercent":0.6}}}}""") };
        var service = new ApiFinanceTrackingService(api);
        var plan = await service.GetPlanAsync(PlanId);
        var request = new SaveTrackingPlanRequest
        {
            Name = "Kısa döngü", ExpectedRevision = 2,
            Definition = TrackingEditorRules.Clone(plan.CurrentVersion!.Definition)
        };
        api.Response = new(HttpStatusCode.OK, "{}");
        await service.UpdatePlanAsync(PlanId, request);
        using var encoded = JsonDocument.Parse(JsonSerializer.Serialize(api.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var execution = encoded.RootElement.GetProperty("definition").GetProperty("execution");
        Assert.Equal(30, execution.GetProperty("maxHoldingMinutes").GetInt32());
        Assert.Equal(0.25m, execution.GetProperty("trailingStopPercent").GetDecimal());
        Assert.Equal(0.6m, execution.GetProperty("trailingStopActivationPercent").GetDecimal());
    }

    [Fact]
    public async Task Paper_position_retains_trailing_stop_and_deadline_from_server()
    {
        var api = new RecordingApiClient { Response = new(HttpStatusCode.OK,
            """{"positions":[{"openedAtUtc":"2026-09-13T10:00:00Z","exitDeadlineUtc":"2026-09-13T10:30:00Z","highWaterPrice":101.25,"trailingStopActivationPrice":100.6,"trailingStopPrice":100.996875,"lastMarkAsOfUtc":"2026-09-13T10:05:00Z"}]}""") };
        var paper = await new ApiFinanceTrackingService(api).GetPaperAsync(PlanId);
        var position = Assert.Single(paper.Positions);
        Assert.Equal(DateTime.Parse("2026-09-13T10:00:00Z").ToUniversalTime(), position.OpenedAtUtc);
        Assert.Equal(DateTime.Parse("2026-09-13T10:30:00Z").ToUniversalTime(), position.ExitDeadlineUtc);
        Assert.Equal(101.25m, position.HighWaterPrice);
        Assert.Equal(100.6m, position.TrailingStopActivationPrice);
        Assert.Equal(100.996875m, position.TrailingStopPrice);
        Assert.Equal(DateTime.Parse("2026-09-13T10:05:00Z").ToUniversalTime(), position.LastMarkAsOfUtc);
    }

    [Fact]
    public void Validation_keeps_structural_errors_separate_from_activation_blockers()
    {
        var response = new PlatformApiResponse(HttpStatusCode.OK,
            "{\"isValid\":true,\"canActivate\":false,\"errors\":[],\"activationBlockers\":[{\"path\":\"execution\",\"code\":\"scheduler_disabled\",\"message\":\"Scheduler unavailable\"}]}");
        var validation = response.ReadJson<TrackingPlanValidationResult>()!;
        Assert.True(validation.IsValid);
        Assert.False(validation.CanActivate);
        Assert.Empty(validation.Errors);
        Assert.Equal("scheduler_disabled", Assert.Single(validation.ActivationBlockers).Code);
    }

    private static async Task InvokeAsync(IFinanceTrackingService service, string operation, CancellationToken cancellationToken)
    {
        switch (operation)
        {
            case "capabilities": await service.GetCapabilitiesAsync(cancellationToken); break;
            case "strategy-results": await service.GetStrategyResultsAsync(cancellationToken: cancellationToken); break;
            case "indicators": await service.GetIndicatorsAsync(cancellationToken); break;
            case "providers": await service.GetProvidersAsync(cancellationToken); break;
            case "validate": await service.ValidateAsync(new(), cancellationToken); break;
            case "plans": await service.GetPlansAsync(cancellationToken: cancellationToken); break;
            case "plan": await service.GetPlanAsync(PlanId, cancellationToken); break;
            case "versions": await service.GetVersionsAsync(PlanId, 2, 20, cancellationToken); break;
            case "version": await service.GetVersionAsync(PlanId, 3, cancellationToken); break;
            case "create": await service.CreatePlanAsync(new(), cancellationToken); break;
            case "update": await service.UpdatePlanAsync(PlanId, new() { ExpectedRevision = 12 }, cancellationToken); break;
            case "status": await service.ChangeStatusAsync(PlanId, new() { ExpectedRevision = 12, Status = TrackingPlanStatus.Paused }, cancellationToken); break;
            case "import": await service.ImportInstrumentAsync(new() { ProviderKey = "binance-spot", ProviderSymbol = "BTCUSDT" }, cancellationToken); break;
            case "preview": await service.PreviewAsync(PlanId, cancellationToken); break;
            case "evaluations": await service.GetEvaluationsAsync(PlanId, 2, 20, cancellationToken); break;
            case "signals": await service.GetSignalsAsync(PlanId, 2, 20, cancellationToken); break;
            case "paper": await service.GetPaperAsync(PlanId, cancellationToken); break;
            case "trades": await service.GetPaperTradesAsync(PlanId, 2, 20, cancellationToken); break;
            case "notifications": await service.GetNotificationsAsync(PlanId, true, 2, 20, cancellationToken); break;
            case "read": await service.MarkNotificationReadAsync(NotificationId, cancellationToken); break;
            default: throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private sealed class RecordingApiClient : IPlatformApiClient
    {
        public HttpMethod? Method { get; private set; }
        public string? Path { get; private set; }
        public IReadOnlyDictionary<string, string?>? Query { get; private set; }
        public object? Body { get; private set; }
        public Guid? OwnerAccountId { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
        public int Calls { get; private set; }
        public PlatformApiResponse Response { get; set; } = new(HttpStatusCode.OK, "{}");
        public Exception? Failure { get; init; }

        public Task<PlatformApiResponse> SendAsync(HttpMethod method, string relativePath,
            IReadOnlyDictionary<string, string?>? query = null, object? body = null,
            Guid? ownerAccountId = null, CancellationToken cancellationToken = default)
        {
            Calls++;
            Method = method;
            Path = relativePath;
            Query = query;
            Body = body;
            OwnerAccountId = ownerAccountId;
            CancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Failure is null ? Task.FromResult(Response) : Task.FromException<PlatformApiResponse>(Failure);
        }

        public Task<T?> GetAsync<T>(string relativePath, IReadOnlyDictionary<string, string?>? query = null,
            Guid? ownerAccountId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<TResponse?> PostAsync<TRequest, TResponse>(string relativePath, TRequest request,
            Guid? ownerAccountId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
