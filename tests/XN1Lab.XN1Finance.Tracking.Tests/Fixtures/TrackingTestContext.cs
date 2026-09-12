using Bunit;
using Microsoft.Extensions.DependencyInjection;
using XN1Lab.Platform.Identity.Abstractions;
using XN1Lab.Platform.Identity.Models;
using XN1Lab.Platform.UI.Hosting;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.PortfolioAnalytics.Services;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.PortfolioAnalytics.Components;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Services;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Shell;
namespace XN1Lab.XN1Finance.Tracking.Tests.Fixtures;
public sealed class TrackingTestContext : BunitContext
{
 public TrackingFixtureService Api {get;}=new();
 public TestPermissions Access {get;}=new();
 public TrackingTestContext(){JSInterop.Mode=JSRuntimeMode.Loose;Services.AddLogging();Services.AddPlatformUi();Services.AddSingleton<IFinanceTrackingService>(Api);Services.AddSingleton<IAccountPermissionResolver>(Access);Services.AddSingleton<ICurrentAccountAccessor>(Access);Services.AddScoped<PortfolioAnalyticsPermissionState>();ComponentFactories.AddStub<PortfolioAnalyticsModuleTabs>();}
 public sealed class TestPermissions : IAccountPermissionResolver, ICurrentAccountAccessor
 {
  public bool Read {get;set;}=true;public bool Write {get;set;}=true;
  public CurrentAccount? Current=>null;public bool IsAuthenticated=>true;
  public bool HasPermission(CurrentAccount? account,string permission)=>permission==PortfolioAnalyticsPermissions.Write?Write:Read;
  public bool HasAnyPermission(CurrentAccount? account,IEnumerable<string> permissions)=>permissions.Any(p=>HasPermission(account,p));
  public bool HasAllPermissions(CurrentAccount? account,IEnumerable<string> permissions)=>permissions.All(p=>HasPermission(account,p));
 }
}
