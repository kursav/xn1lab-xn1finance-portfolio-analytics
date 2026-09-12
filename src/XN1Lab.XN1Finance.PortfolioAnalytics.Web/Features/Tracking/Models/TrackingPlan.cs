using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;

public sealed class TrackingPlan
{
    public Guid Id { get; set; }
    public Guid OwnerAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TrackingPlanStatus Status { get; set; } = TrackingPlanStatus.Draft;
    public int CurrentVersionNumber { get; set; }
    public Guid CurrentVersionId { get; set; }
    public long Revision { get; set; }
    public long ExecutionGeneration { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public TrackingPlanVersion? CurrentVersion { get; set; }
}

public sealed class TrackingPlanVersion
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public Guid OwnerAccountId { get; set; }
    public int VersionNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TrackingPlanDefinition Definition { get; set; } = new();
    /// <summary>Pins the exact indicator metadata used to validate this version.</summary>
    public List<IndicatorDefinition> IndicatorSnapshots { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class SaveTrackingPlanRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    [StringLength(4000)]
    public string? Description { get; set; }
    [Required]
    public TrackingPlanDefinition Definition { get; set; } = new();
    /// <summary>Required on PUT; create always begins with revision 1.</summary>
    public long? ExpectedRevision { get; set; }
}

public sealed class ChangeTrackingPlanStatusRequest
{
    [JsonRequired]
    public TrackingPlanStatus Status { get; set; }
    [Range(1, long.MaxValue)]
    public long ExpectedRevision { get; set; }
}

public sealed class TrackingValidationIssue
{
    public string Path { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class TrackingPlanValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public bool CanActivate => IsValid && ActivationBlockers.Count == 0;
    public List<TrackingValidationIssue> Errors { get; set; } = [];
    public List<TrackingValidationIssue> ActivationBlockers { get; set; } = [];
}

public sealed class TrackingCapabilities
{
    public int DefinitionSchemaVersion { get; set; } = 1;
    public bool PlanManagementAvailable { get; set; } = true;
    public bool EvaluationAvailable { get; set; }
    public bool SchedulerAvailable { get; set; }
    public List<string> NotificationChannels { get; set; } = [];
    public bool NotificationsAvailable { get; set; }
    public bool PaperTradingAvailable { get; set; }
    public bool LiveTradingAvailable { get; set; }
    public string ScoreMethod { get; set; } = "WeightedConditionalScoreV1";
    public decimal MinimumScore { get; set; } = -100;
    public decimal MaximumScore { get; set; } = 100;
    public List<TrackingTimeframe> Timeframes { get; set; } = [.. Enum.GetValues<TrackingTimeframe>()];
}
