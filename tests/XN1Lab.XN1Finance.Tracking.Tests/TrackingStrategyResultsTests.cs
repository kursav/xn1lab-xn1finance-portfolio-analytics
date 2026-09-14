using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using XN1Lab.Platform.Api.Exceptions;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Components;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Pages;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Services;
using XN1Lab.XN1Finance.Tracking.Tests.Fixtures;

namespace XN1Lab.XN1Finance.Tracking.Tests;

public sealed class TrackingStrategyResultsTests
{
    [Fact]
    public void Results_load_only_when_opened_and_do_not_require_a_selected_plan()
    {
        using var ctx = new TrackingTestContext();
        var cut = ctx.Render<TrackingPlansPage>();
        Assert.DoesNotContain("strategy-results", ctx.Api.Calls);
        cut.Find("[data-testid=tracking-results]").Click();
        cut.WaitForAssertion(() => Assert.Contains("strategy-results", ctx.Api.Calls));
        Assert.Equal(2, cut.FindAll("tr[data-plan-version]").Count);
        Assert.DoesNotContain("plan", ctx.Api.Calls);
        Assert.Empty(ctx.Api.StatusRequests);
    }

    [Fact]
    public void Server_direction_rates_keep_measured_denominators_and_separate_paper_net_results()
    {
        using var ctx = new TrackingTestContext();
        var cut = ctx.Render<TrackingStrategyResults>();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("tr[data-plan-version]").Count));
        var first = cut.Find($"tr[data-plan-version='{StrategyResultsFixture.VersionId}']");
        Assert.Contains("%62,5", first.TextContent);
        Assert.Contains("5 / 8 ölçüm", first.TextContent);
        Assert.Contains("1 bekliyor · 1 eksik", first.TextContent);
        Assert.Contains("Kapsam: 8 / 10", first.TextContent);
        Assert.Contains("1 referans yok", first.TextContent);
        Assert.Contains("1 / 4 kârlı işlem", first.TextContent);
        Assert.Contains("%25", first.TextContent);
        Assert.Contains("-3,75 USDT", first.TextContent);
        Assert.Contains("Az örnek", first.TextContent);
    }

    [Fact]
    public void Zero_measured_and_zero_closed_episodes_show_no_percentage_or_fake_profit()
    {
        using var ctx = new TrackingTestContext();
        var cut = ctx.Render<TrackingStrategyResults>();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("tr[data-plan-version]").Count));
        var empty = cut.FindAll("tr[data-plan-version]")[1];
        Assert.DoesNotContain("%", empty.TextContent);
        Assert.DoesNotContain("0,00 USDT", empty.TextContent);
        Assert.Contains("0 / 0 ölçüm", empty.TextContent);
        Assert.Contains("0 / 0 kârlı işlem", empty.TextContent);
    }

    [Fact]
    public void Expanded_paired_details_keep_versioned_indicators_and_actual_exits_without_duplicate_condition_results()
    {
        using var ctx = new TrackingTestContext();
        var cut = ctx.Render<TrackingStrategyResults>();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("tr[data-plan-version]").Count));
        cut.FindAll("tr[data-plan-version] button")[0].Click();
        var detail = cut.Find(".results-detail");
        Assert.Contains("RSI (14)", detail.TextContent);
        Assert.Equal(3, cut.FindAll(".results-indicators > span").Count);
        Assert.All(cut.FindAll(".results-indicators > span"), indicator => Assert.Contains("RSI (14)", indicator.TextContent));
        Assert.DoesNotContain("EMA", detail.TextContent);
        Assert.Contains("5 dakika", detail.TextContent);
        Assert.Contains("1 saat", detail.TextContent);
        Assert.Contains("2 saat", detail.TextContent);
        Assert.Contains("Filtre katkısı", detail.TextContent);
        Assert.Empty(cut.FindAll(".results-conditions"));
        Assert.DoesNotContain("tek başına indikatör etkisi değildir", detail.TextContent);
        Assert.Contains("Süre doldu", detail.TextContent);
        Assert.Contains("İz süren stop", detail.TextContent);
    }

    [Fact]
    public void Legacy_plan_details_keep_conditional_measurements_when_paired_report_is_absent()
    {
        using var ctx = new TrackingTestContext();
        ctx.Services.AddSingleton<IFinanceTrackingService>(new RecordingResultsService { WithoutFilterComparison = true });
        var cut = ctx.Render<TrackingStrategyResults>();
        cut.FindAll("tr[data-plan-version] button")[0].Click();
        var detail = cut.Find(".results-detail");
        Assert.Contains("4 / 6 ölçüm", detail.TextContent);
        Assert.Contains("%66,7", detail.TextContent);
        Assert.Contains("tek başına indikatör etkisi değildir", detail.TextContent);
        Assert.Single(cut.FindAll(".results-conditions"));
        Assert.Empty(cut.FindAll(".filter-comparison"));
    }

    [Fact]
    public void Period_change_requests_new_server_aggregates_instead_of_filtering_visible_rows()
    {
        using var ctx = new TrackingTestContext();
        var api = new RecordingResultsService();
        ctx.Services.AddSingleton<IFinanceTrackingService>(api);
        var cut = ctx.Render<TrackingStrategyResults>();
        cut.WaitForAssertion(() => Assert.Single(api.Periods));
        Assert.InRange(api.Periods[0], TimeSpan.FromDays(7), TimeSpan.FromDays(7).Add(TimeSpan.FromSeconds(1)));
        Assert.Null(api.ToUtcValues[0]);
        cut.Find("select[aria-label='Sonuç dönemi']").Change("1");
        cut.WaitForAssertion(() => Assert.Equal(2, api.Periods.Count));
        Assert.InRange(api.Periods[1], TimeSpan.FromDays(1), TimeSpan.FromDays(1).Add(TimeSpan.FromSeconds(1)));
        Assert.Null(api.ToUtcValues[1]);
    }

    [Fact]
    public void API_failure_shows_retry_error_without_sample_results()
    {
        using var ctx = new TrackingTestContext();
        var api = new RecordingResultsService { Fail = true };
        ctx.Services.AddSingleton<IFinanceTrackingService>(api);
        var cut = ctx.Render<TrackingStrategyResults>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[role=alert]")));
        Assert.Empty(cut.FindAll("tr[data-plan-version]"));
        Assert.DoesNotContain("karşılaştırılacak kayıt yok", cut.Markup);
        api.Fail = false;
        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("tr[data-plan-version]").Count));
    }

    private sealed class RecordingResultsService : TrackingFixtureService
    {
        public List<TimeSpan> Periods { get; } = [];
        public List<DateTime?> ToUtcValues { get; } = [];
        public bool Fail { get; set; }
        public bool WithoutFilterComparison { get; set; }
        public override async Task<FinanceStrategyResults> GetStrategyResultsAsync(DateTime? fromUtc = null, DateTime? toUtc = null, IReadOnlyList<Guid>? planIds = null, CancellationToken cancellationToken = default)
        {
            if (Fail) throw new PlatformApiException(HttpStatusCode.ServiceUnavailable, "");
            Periods.Add(DateTime.UtcNow - fromUtc!.Value);
            ToUtcValues.Add(toUtc);
            var results = await base.GetStrategyResultsAsync(fromUtc, toUtc, planIds, cancellationToken);
            if (WithoutFilterComparison) foreach (var plan in results.Plans) plan.RsiFilterComparison = null;
            return results;
        }
    }
}
