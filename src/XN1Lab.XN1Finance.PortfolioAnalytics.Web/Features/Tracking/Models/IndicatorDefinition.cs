namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;

public sealed class IndicatorDefinition
{
    public string Code { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CalculatorKey { get; set; } = string.Empty;
    public string CalculationKind { get; set; } = "BuiltIn";
    public List<IndicatorParameterDefinition> Parameters { get; set; } = [];
    public List<IndicatorOutputDefinition> Outputs { get; set; } = [];
    public List<TrackingPriceField> SupportedSources { get; set; } = [TrackingPriceField.Close];
    public List<TrackingTimeframe> SupportedTimeframes { get; set; } = [.. Enum.GetValues<TrackingTimeframe>()];
    public string WarmupPolicy { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public sealed class IndicatorParameterDefinition
{
    public string Key { get; set; } = string.Empty;
    public decimal Minimum { get; set; }
    public decimal Maximum { get; set; }
    public decimal Default { get; set; }
    public bool IntegerOnly { get; set; } = true;
    public bool Required { get; set; } = true;
}

public sealed class IndicatorOutputDefinition
{
    public string Key { get; set; } = "value";
    public string Unit { get; set; } = string.Empty;
}
