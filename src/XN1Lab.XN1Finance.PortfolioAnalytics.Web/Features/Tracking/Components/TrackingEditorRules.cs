using System.Text.Json;
using XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Components;

/// <summary>Pure editor transformations. Editing never mutates the plan returned by the API.</summary>
public static class TrackingEditorRules
{
    public static TrackingPlanDefinition Clone(TrackingPlanDefinition definition) =>
        JsonSerializer.Deserialize<TrackingPlanDefinition>(JsonSerializer.Serialize(definition))!;

    public static string Timeframe(TrackingTimeframe value) => value switch
    {
        TrackingTimeframe.Minute5 => "5 dakika", TrackingTimeframe.Minute15 => "15 dakika",
        TrackingTimeframe.Minute30 => "30 dakika", TrackingTimeframe.Hour1 => "1 saat",
        TrackingTimeframe.Hour2 => "2 saat", TrackingTimeframe.Hour4 => "4 saat",
        TrackingTimeframe.Day1 => "Günlük", TrackingTimeframe.Week1 => "Haftalık", _ => value.ToString()
    };

    public static string PriceField(TrackingPriceField value) => value switch
    {
        TrackingPriceField.Open => "Açılış", TrackingPriceField.High => "En yüksek",
        TrackingPriceField.Low => "En düşük", TrackingPriceField.Close => "Kapanış",
        TrackingPriceField.Volume => "Hacim", _ => value.ToString()
    };

    public static string Comparison(TrackingComparison value) => value switch
    {
        TrackingComparison.LessThan => "Küçüktür (<)",
        TrackingComparison.LessThanOrEqual => "Küçük veya eşittir (≤)",
        TrackingComparison.GreaterThan => "Büyüktür (>)",
        TrackingComparison.GreaterThanOrEqual => "Büyük veya eşittir (≥)",
        TrackingComparison.CrossesAbove => "Yukarı keser (↑)",
        TrackingComparison.CrossesBelow => "Aşağı keser (↓)", _ => value.ToString()
    };

    public static TrackingRule Compare(TrackingValue left, TrackingComparison comparison, TrackingValue right) =>
        new() { Kind = TrackingRuleKind.Comparison, Left = left, Comparison = comparison, Right = right };
    public static TrackingValue Constant(decimal value) => new() { Kind = TrackingValueKind.Constant, Constant = value };
    public static TrackingValue Indicator(string key, string output = "value") =>
        new() { Kind = TrackingValueKind.Indicator, IndicatorBindingKey = key, OutputKey = output };
    public static TrackingRule EmptyComparison() => Compare(Constant(0), TrackingComparison.GreaterThan, Constant(0));

    public static TrackingPlanDefinition NewDefinition() => new()
    {
        Indicators =
        [
            new() { BindingKey = "rsi2h", IndicatorCode = "RSI", IndicatorVersion = 1,
                Timeframe = TrackingTimeframe.Hour2, Parameters = new() { ["period"] = 14 } },
            new() { BindingKey = "emaDaily", IndicatorCode = "EMA", IndicatorVersion = 1,
                Timeframe = TrackingTimeframe.Day1, Parameters = new() { ["period"] = 50 } }
        ],
        ScoreComponents =
        [
            new() { Key = "oversold", WeightPercent = 60,
                Condition = Compare(Indicator("rsi2h"), TrackingComparison.LessThan, Constant(30)), WhenTrueScore = 100 },
            new() { Key = "dailyTrend", WeightPercent = 40,
                Condition = Compare(new() { Kind = TrackingValueKind.Price, Timeframe = TrackingTimeframe.Day1,
                    PriceField = TrackingPriceField.Close }, TrackingComparison.GreaterThan, Indicator("emaDaily")), WhenTrueScore = 100 }
        ],
        EntryRule = Compare(new() { Kind = TrackingValueKind.TotalScore }, TrackingComparison.GreaterThanOrEqual, Constant(100)),
        ExitRule = Compare(new() { Kind = TrackingValueKind.TotalScore }, TrackingComparison.LessThanOrEqual, Constant(25)),
        Execution = new() { Mode = TrackingExecutionMode.Observe, QuoteCurrency = "USDT" }
    };

    public static IEnumerable<TrackingRule> Rules(TrackingPlanDefinition definition)
    {
        foreach (var root in definition.ScoreComponents.Select(x => x.Condition).Concat([definition.EntryRule, definition.ExitRule]))
            foreach (var rule in Descendants(root)) yield return rule;
    }

    private static IEnumerable<TrackingRule> Descendants(TrackingRule? rule, int depth = 1)
    {
        if (rule is null || depth > 8) yield break;
        yield return rule;
        foreach (var child in rule.Children)
            foreach (var descendant in Descendants(child, depth + 1)) yield return descendant;
    }

    public static bool References(TrackingPlanDefinition definition, string key) => Rules(definition)
        .SelectMany(rule => new[] { rule.Left, rule.Right })
        .Any(value => value?.Kind == TrackingValueKind.Indicator && value.IndicatorBindingKey == key);

    public static void RenameBinding(TrackingPlanDefinition definition, TrackingIndicator binding, string newKey)
    {
        var previous = binding.BindingKey;
        foreach (var value in Rules(definition).SelectMany(rule => new[] { rule.Left, rule.Right }))
            if (value?.Kind == TrackingValueKind.Indicator && value.IndicatorBindingKey == previous)
                value.IndicatorBindingKey = newKey;
        binding.BindingKey = newKey;
    }
}
