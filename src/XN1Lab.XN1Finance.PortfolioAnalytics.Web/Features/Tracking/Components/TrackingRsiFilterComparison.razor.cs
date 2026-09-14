using System.Globalization;
using Microsoft.AspNetCore.Components;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Components;

public partial class TrackingRsiFilterComparison
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly string[] VariantKeys = ["none", "hour1", "hour2", "both"];
    private static readonly bool[] Groups = [true, false];
    private int Horizon = 5;

    [Parameter, EditorRequired] public FinanceStrategyResult Plan { get; set; } = default!;

    private static string Rate(decimal? value) => value.HasValue ? $"%{value.Value.ToString("0.##", Turkish)}" : "—";
    private static string Utc(DateTime value) => value == default ? "—" : value.ToString("dd.MM.yyyy HH:mm", Turkish);
    private static string FilterName(string key) => key switch
    {
        "none" => "Filtresiz", "hour1" => "1 saat RSI > 50", "hour2" => "2 saat RSI > 50", "both" => "1 + 2 saat RSI > 50", _ => key
    };

    private static string PopulationGaps(FinanceRsiFilterComparison comparison)
    {
        List<string> parts = [];
        if (comparison.ExcludedOpportunityCount > 0) parts.Add($"{comparison.ExcludedOpportunityCount} fırsat kapsam dışı");
        if (comparison.UnknownBaseDecisionCount > 0) parts.Add($"{comparison.UnknownBaseDecisionCount} temel koşul belirsiz");
        if (comparison.MissingIndicatorDecisionCount > 0) parts.Add($"{comparison.MissingIndicatorDecisionCount} karar indikatörü eksik");
        if (comparison.InvalidIndicatorDecisionCount > 0) parts.Add($"{comparison.InvalidIndicatorDecisionCount} karar indikatörü geçersiz");
        if (comparison.DuplicateDecisionCount > 0) parts.Add($"{comparison.DuplicateDecisionCount} tekrar dışlandı");
        if (comparison.InvalidDecisionCount > 0) parts.Add($"{comparison.InvalidDecisionCount} karar kaydı geçersiz");
        return string.Join(" · ", parts);
    }

    private static string OutcomeGaps(FinanceRsiOutcomeStatistics outcome)
    {
        List<string> parts = [];
        if (outcome.Pending > 0) parts.Add($"{outcome.Pending} bekliyor");
        if (outcome.Missing > 0) parts.Add($"{outcome.Missing} eksik");
        if (outcome.NoReference > 0) parts.Add($"{outcome.NoReference} referans yok");
        if (outcome.Invalid > 0) parts.Add($"{outcome.Invalid} geçersiz");
        return parts.Count == 0 ? "—" : string.Join(" · ", parts);
    }
}
