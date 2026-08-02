namespace NJobDesk.Core.Models;

public record JobDetailModel
{
    public required JobSummaryModel Job { get; init; }

    public IReadOnlyList<TriggerModel> Triggers { get; init; } = [];

    public IReadOnlyList<ExecutionModel> RecentExecutions { get; init; } = [];
}
