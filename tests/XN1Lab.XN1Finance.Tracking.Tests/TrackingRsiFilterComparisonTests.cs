using Bunit;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Components;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;
using XN1Lab.XN1Finance.Tracking.Tests.Fixtures;

namespace XN1Lab.XN1Finance.Tracking.Tests;

public sealed class TrackingRsiFilterComparisonTests
{
    [Fact]
    public void Details_show_same_population_source_version_gross_direction_and_partial_coverage()
    {
        using var ctx = new TrackingTestContext();
        var cut = ctx.Render<TrackingStrategyResults>();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("tr[data-plan-version]").Count));
        cut.FindAll("tr[data-plan-version] button")[0].Click();
        var report = cut.Find(".filter-comparison");
        Assert.Equal(StrategyResultsFixture.VersionId.ToString(), report.GetAttribute("data-source-version"));
        Assert.Contains("Kaynak: Örnek · RSI toparlanma · v2", report.TextContent);
        Assert.Contains("Gözlenen karar: 100", report.TextContent);
        Assert.Contains("Temel fırsat: 12 · Ortak karşılaştırma: 10", report.TextContent);
        Assert.Contains("Bekle kararları dahil", report.TextContent);
        Assert.Contains("Brüt fiyat hareketi; sanal işlem kârı değildir", report.TextContent);
        Assert.Contains("2 fırsat kapsam dışı", report.TextContent);
        Assert.Contains("2 temel koşul belirsiz", report.TextContent);
        Assert.Contains("3 karar indikatörü eksik", report.TextContent);
        Assert.Contains("1 karar indikatörü geçersiz", report.TextContent);
        Assert.Contains("1 karar kaydı geçersiz", report.TextContent);
        Assert.Contains("1 tekrar dışlandı", report.TextContent);
        Assert.Equal(8, cut.FindAll(".filter-comparison tr[data-group]").Count);
        var none = cut.Find("tr[data-filter=none][data-group=accepted]");
        Assert.Contains("8 / 10", none.TextContent);
        Assert.Contains("Kapsam: %80", none.TextContent);
        Assert.Contains("4 / 3 / 1", none.TextContent);
        Assert.Contains("Yükseliş: %50", none.TextContent);
        Assert.Contains("1 bekliyor · 1 eksik", none.TextContent);
        Assert.Contains("Canlı: 3 · Karar kaydı: 3 · Belirsiz: 2", none.TextContent);
        Assert.DoesNotContain("Az örnek", report.TextContent);
        Assert.DoesNotContain("USDT", report.TextContent);
        Assert.Contains("-3,75 USDT", cut.Find("tr[data-plan-version]").TextContent);
    }

    [Fact]
    public void Horizon_selector_updates_both_groups_without_new_api_request()
    {
        using var ctx = new TrackingTestContext();
        var cut = ctx.Render<TrackingStrategyResults>();
        cut.FindAll("tr[data-plan-version] button")[0].Click();
        cut.Find("select[aria-label='Filtre sonucu süresi']").Change("30");
        var none = cut.Find("tr[data-filter=none][data-group=accepted]");
        Assert.Contains("3 / 10", none.TextContent);
        Assert.Contains("Kapsam: %30", none.TextContent);
        Assert.Contains("2 / 1 / 0", none.TextContent);
        Assert.Contains("Yükseliş: %66,67", none.TextContent);
        Assert.Contains("3 bekliyor · 2 eksik · 1 referans yok · 1 geçersiz", none.TextContent);
        Assert.Contains("Canlı: 3 · Karar kaydı: 0 · Belirsiz: 0", none.TextContent);
        Assert.Single(ctx.Api.Calls, call => call == "strategy-results");
        cut.Find("select[aria-label='Filtre sonucu süresi']").Change("15");
        Assert.Contains("6 / 10", cut.Find("tr[data-filter=none][data-group=accepted]").TextContent);
        Assert.Contains("2 bekliyor · 1 eksik · 1 referans yok", cut.Find("tr[data-filter=none][data-group=accepted]").TextContent);
    }

    [Fact]
    public void Null_optional_report_keeps_existing_plan_details_usable()
    {
        using var ctx = new TrackingTestContext();
        var cut = ctx.Render<TrackingStrategyResults>();
        cut.FindAll("tr[data-plan-version] button")[1].Click();
        Assert.Empty(cut.FindAll(".filter-comparison"));
        Assert.Contains("İndikatörler", cut.Find(".results-detail").TextContent);
    }

    [Fact]
    public void Empty_observed_population_has_no_table_or_invented_percentages()
    {
        using var ctx = new TrackingTestContext();
        var plan = Plan();
        plan.RsiFilterComparison = new();
        var cut = ctx.Render<TrackingRsiFilterComparison>(parameters => parameters.Add(x => x.Plan, plan));
        Assert.Contains("Gözlenen karar: 0", cut.Markup);
        Assert.Contains("Bu dönemde karşılaştırılabilir temel fırsat yok", cut.Markup);
        Assert.Empty(cut.FindAll("table"));
        Assert.DoesNotContain("%", cut.Markup);
        Assert.DoesNotContain("01.01.0001", cut.Markup);
    }

    [Fact]
    public void Missing_variant_or_horizon_is_explicit_and_never_rendered_as_zero_percent()
    {
        using var ctx = new TrackingTestContext();
        var plan = Plan();
        plan.RsiFilterComparison!.Variants.RemoveAll(x => x.Key == "hour2");
        plan.RsiFilterComparison.Variants.First(x => x.Key == "hour1").Rejected.RemoveAll(x => x.HorizonMinutes == 5);
        var cut = ctx.Render<TrackingRsiFilterComparison>(parameters => parameters.Add(x => x.Plan, plan));
        var missingVariant = cut.Find("tr[data-filter=hour2]");
        Assert.Contains("Filtre verisi yok", missingVariant.TextContent);
        Assert.DoesNotContain("%", missingVariant.TextContent);
        var missingHorizon = cut.Find("tr[data-filter=hour1][data-group=rejected]");
        Assert.Contains("Bu süre için ölçüm verisi yok", missingHorizon.TextContent);
        Assert.DoesNotContain("%", missingHorizon.TextContent);
    }

    [Fact]
    public void Zero_eligible_and_zero_measured_do_not_show_success_or_mean_price()
    {
        using var ctx = new TrackingTestContext();
        var plan = Plan();
        var cut = ctx.Render<TrackingRsiFilterComparison>(parameters => parameters.Add(x => x.Plan, plan));
        var empty = cut.Find("tr[data-filter=none][data-group=rejected]");
        Assert.Contains("0 / 0", empty.TextContent);
        Assert.DoesNotContain("%", empty.TextContent);
        var outcome = plan.RsiFilterComparison!.Variants.First(x => x.Key == "hour1").Accepted[0];
        outcome.Measured = 0; outcome.Positive = 0; outcome.Negative = 0; outcome.Neutral = 0;
        outcome.Pending = outcome.Eligible; outcome.CoveragePercent = 0;
        outcome.PositiveRatePercent = 99; outcome.AveragePriceChangePercent = 5;
        cut.Render(parameters => parameters.Add(x => x.Plan, plan));
        var pending = cut.Find("tr[data-filter=hour1][data-group=accepted]");
        Assert.Contains("Kapsam: %0", pending.TextContent);
        Assert.Contains("Yükseliş: —", pending.TextContent);
        Assert.DoesNotContain("%99", pending.TextContent);
        Assert.DoesNotContain("%5", pending.TextContent);
    }

    [Fact]
    public void Preview_fixture_partitions_the_same_opportunities_and_outcomes_for_every_filter()
    {
        var comparison = Plan().RsiFilterComparison!;
        foreach (var variant in comparison.Variants)
        {
            Assert.Equal(comparison.ComparableOpportunityCount, variant.AcceptedOpportunityCount + variant.RejectedOpportunityCount);
            foreach (var horizon in new[] { 5, 15, 30 })
            {
                var accepted = variant.Accepted.Single(x => x.HorizonMinutes == horizon);
                var rejected = variant.Rejected.Single(x => x.HorizonMinutes == horizon);
                var unfiltered = comparison.Variants[0].Accepted.Single(x => x.HorizonMinutes == horizon);
                Assert.Equal(unfiltered.Measured, accepted.Measured + rejected.Measured);
                Assert.Equal(unfiltered.Positive, accepted.Positive + rejected.Positive);
                Assert.Equal(unfiltered.Negative, accepted.Negative + rejected.Negative);
                Assert.Equal(unfiltered.Neutral, accepted.Neutral + rejected.Neutral);
                foreach (var outcome in new[] { accepted, rejected })
                {
                    Assert.Equal(outcome.Eligible, outcome.Measured + outcome.Pending + outcome.Missing + outcome.NoReference + outcome.Invalid);
                    Assert.Equal(outcome.Measured, outcome.LiveQuoteCount + outcome.RecordedDecisionCount + outcome.UnknownProvenanceCount);
                }
            }
        }
    }

    private static FinanceStrategyResult Plan()
    {
        var now = new DateTime(2026, 9, 14, 2, 0, 0, DateTimeKind.Utc);
        return StrategyResultsFixture.Create(now.AddDays(-1), now).Plans[0];
    }
}
