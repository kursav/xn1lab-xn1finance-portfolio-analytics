using Bunit;
using AngleSharp.Dom;
using Microsoft.Extensions.DependencyInjection;
using XN1Lab.Platform.Shared.Pagination;
using XN1Lab.XN1Finance.Tracking.Tests.Fixtures;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Pages;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Services;
namespace XN1Lab.XN1Finance.Tracking.Tests;
public sealed class TrackingWorkspaceTests
{
 [Theory]
 [InlineData("paper_trailing_stop", "İz süren stop")]
 [InlineData("paper_max_holding_time", "Süre doldu")]
 [InlineData("provider_unknown_reason", "provider_unknown_reason")]
 public void Evaluation_status_translates_protection_exits_and_preserves_unknown_reasons(string reason, string expected)
 {
  using var ctx = new TrackingTestContext();
  ctx.Services.AddSingleton<IFinanceTrackingService>(new EvaluationReasonFixture(reason));
  var cut = ctx.Render<TrackingPlansPage>();
  Select(cut);
  Button(cut, "Hesaplama geçmişi").Click();
  cut.WaitForAssertion(() => Assert.Contains(cut.FindAll("tbody td"), cell => cell.TextContent == expected));
 }

 private sealed class EvaluationReasonFixture(string reason) : TrackingFixtureService
 {
  public override Task<PaginatedList<FinanceEvaluation>> GetEvaluationsAsync(Guid planId, int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
   => Task.FromResult(Page(new[] { new FinanceEvaluation
   {
    Id = Guid.NewGuid(), PlanId = planId, InstrumentId = InstrumentId,
    Kind = FinanceEvaluationKind.Protection, ExecutionReason = reason,
    Decision = new() { IsAvailable = true, Action = FinanceDecisionAction.Sell }
   } }, pageNumber));
 }

 static IElement Button(IRenderedComponent<TrackingPlansPage> cut,string text)=>Assert.Single(cut.FindAll("button"),button=>button.TextContent.Trim().Contains(text,StringComparison.Ordinal));
 static void Select(IRenderedComponent<TrackingPlansPage> cut){cut.WaitForAssertion(()=>Assert.Single(cut.FindAll(".tracking-plan-card")));cut.Find(".tracking-plan-card").Click();cut.WaitForAssertion(()=>Assert.Single(cut.FindAll("[data-testid='tracking-edit']")));}
 [Fact] public void Read_permission_is_required_before_any_api_call(){using var ctx=new TrackingTestContext();ctx.Access.Read=false;var cut=ctx.Render<TrackingPlansPage>();Assert.Contains("okuma yetkisi",cut.Markup);Assert.Empty(ctx.Api.Calls);}
 [Fact] public void Readonly_user_can_preview_but_cannot_mutate_or_activate(){using var ctx=new TrackingTestContext();ctx.Access.Write=false;var cut=ctx.Render<TrackingPlansPage>();Select(cut);Assert.True(Button(cut,"Yeni plan").HasAttribute("disabled"));Assert.True(Button(cut,"Planı düzenle").HasAttribute("disabled"));Assert.True(Button(cut,"Otomatik takibi başlat").HasAttribute("disabled"));Button(cut,"Kararı hesapla").Click();cut.WaitForAssertion(()=>Assert.Contains("preview",ctx.Api.Calls));Assert.Empty(ctx.Api.StatusRequests);Assert.Null(ctx.Api.SavedRequest);}
 [Fact] public void Preview_shows_weighted_breakdown_without_activation_or_saving(){using var ctx=new TrackingTestContext();var cut=ctx.Render<TrackingPlansPage>();Select(cut);Button(cut,"Kararı hesapla").Click();cut.WaitForAssertion(()=>Assert.Contains("Puanın oluşumu",cut.Markup));Assert.Contains("rsi2h",cut.Markup);Assert.Empty(ctx.Api.StatusRequests);Assert.DoesNotContain("save",ctx.Api.Calls);}
 [Fact] public void Overview_only_shows_plain_execution_mode(){using var ctx=new TrackingTestContext();var cut=ctx.Render<TrackingPlansPage>();Select(cut);var mode=Assert.Single(cut.FindAll(".tracking-mode-line"));Assert.Contains("Çalışma modu:",mode.TextContent);Assert.Contains("Sanal işlem",mode.TextContent);Assert.DoesNotContain("Karar zaman dilimi",cut.Markup);Assert.DoesNotContain("Takip sepeti",cut.Markup);Assert.DoesNotContain("İndikatör bağlantıları",cut.Markup);Assert.DoesNotContain("Puan ağırlıkları",cut.Markup);}
 [Fact] public void Activation_requires_validation_then_explicit_confirmation_and_loaded_revision(){using var ctx=new TrackingTestContext();var cut=ctx.Render<TrackingPlansPage>();Select(cut);Button(cut,"Otomatik takibi başlat").Click();cut.WaitForAssertion(()=>Assert.Contains("validate",ctx.Api.Calls));Assert.Empty(ctx.Api.StatusRequests);Button(cut,"Evet, otomatik takibi başlat").Click();cut.WaitForAssertion(()=>Assert.Single(ctx.Api.StatusRequests));Assert.Equal(7,ctx.Api.StatusRequests[0].ExpectedRevision);Assert.Equal(TrackingPlanStatus.Active,ctx.Api.StatusRequests[0].Status);}
 [Fact] public void Validation_errors_block_activation_and_show_reason(){using var ctx=new TrackingTestContext();ctx.Api.Invalid=true;var cut=ctx.Render<TrackingPlansPage>();Select(cut);Button(cut,"Otomatik takibi başlat").Click();cut.WaitForAssertion(()=>Assert.Contains("weight_total",cut.Markup));Assert.True(Button(cut,"Evet, otomatik takibi başlat").HasAttribute("disabled"));Assert.Empty(ctx.Api.StatusRequests);}
 [Fact] public void Disabled_scheduler_keeps_activation_disabled_and_preview_available(){using var ctx=new TrackingTestContext();ctx.Api.SchedulerEnabled=false;var cut=ctx.Render<TrackingPlansPage>();Select(cut);Assert.Contains("Otomatik tarama servisi kapalı",cut.Markup);Assert.True(Button(cut,"Otomatik takibi başlat").HasAttribute("disabled"));Assert.False(Button(cut,"Kararı hesapla").HasAttribute("disabled"));}
 [Fact] public void Missing_backend_is_an_error_not_a_fake_empty_portfolio(){using var ctx=new TrackingTestContext();ctx.Api.LoadFailure=new InvalidOperationException("Backend missing");var cut=ctx.Render<TrackingPlansPage>();cut.WaitForAssertion(()=>Assert.NotEmpty(cut.FindAll("[role=alert]")));Assert.DoesNotContain("9.000",cut.Markup);Assert.Empty(ctx.Api.StatusRequests);}
 [Fact] public void Empty_list_has_a_useful_starting_state_and_no_made_up_plans(){using var ctx=new TrackingTestContext();ctx.Api.Empty=true;var cut=ctx.Render<TrackingPlansPage>();cut.WaitForAssertion(()=>Assert.Contains("plans",ctx.Api.Calls));Assert.Empty(cut.FindAll(".tracking-plan-card"));Assert.False(Button(cut,"Yeni plan").HasAttribute("disabled"));}
 [Fact] public void Readonly_notification_read_receipt_is_disabled(){using var ctx=new TrackingTestContext();ctx.Access.Write=false;var cut=ctx.Render<TrackingPlansPage>();Select(cut);Button(cut,"Bildirimler").Click();cut.WaitForAssertion(()=>Assert.Contains("Sanal alım tamamlandı",cut.Markup));Assert.True(Button(cut,"Okundu").HasAttribute("disabled"));Assert.DoesNotContain("read",ctx.Api.Calls);}
 [Fact] public void Writable_user_can_mark_a_notification_read(){using var ctx=new TrackingTestContext();var cut=ctx.Render<TrackingPlansPage>();Select(cut);Button(cut,"Bildirimler").Click();cut.WaitForAssertion(()=>Assert.Contains("Sanal alım tamamlandı",cut.Markup));Button(cut,"Okundu").Click();cut.WaitForAssertion(()=>Assert.Contains("read",ctx.Api.Calls));Assert.True(ctx.Api.NotificationRead);}
 [Fact] public void Active_plan_shows_compact_header_actions_before_tabs(){using var ctx=new TrackingTestContext();ctx.Api.Plan.Status=TrackingPlanStatus.Active;var cut=ctx.Render<TrackingPlansPage>();Select(cut);Assert.Single(cut.FindAll(".tracking-detail-header + .tracking-header-actions"));Assert.NotNull(Button(cut,"Kararı hesapla"));Assert.NotNull(Button(cut,"Taramayı durdur"));Assert.Empty(cut.FindAll(".tracking-command-card"));Assert.DoesNotContain("PLAN SONUÇLARI VE GEÇMİŞİ",cut.Markup);}
 [Fact] public void Archived_plan_cannot_be_edited_or_reactivated(){using var ctx=new TrackingTestContext();ctx.Api.Plan.Status=TrackingPlanStatus.Archived;var cut=ctx.Render<TrackingPlansPage>();Select(cut);Assert.True(Button(cut,"Planı düzenle").HasAttribute("disabled"));Assert.DoesNotContain(cut.FindAll("button"),button=>button.TextContent.Contains("Otomatik takibi başlat"));}
}
