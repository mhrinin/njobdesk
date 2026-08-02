using NJobDesk.Core.Providers;

namespace NJobDesk.Core.Models;

public record JobSummaryModel
{
    public required string Id { get; init; }

    public string ProviderKey { get; init; } = string.Empty;

    public string? Group { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public string? JobType { get; init; }

    public bool? Durable { get; init; }

    public bool? ConcurrentExecutionDisallowed { get; init; }

    public int TriggerCount { get; init; }

    public string? ScheduleSummary { get; init; }

    public required JobState State { get; init; }

    public DateTime? NextFireTimeUtc { get; init; }

    public DateTime? PreviousFireTimeUtc { get; init; }

    public bool IsSystemJob { get; init; }

    public required SchedulerCapabilities Capabilities { get; init; }
}
