namespace NJobDesk.Core.Models;

public record CronValidationResultModel
{
    public required bool IsValid { get; init; }

    public string? Error { get; init; }

    public string? Summary { get; init; }

    public IReadOnlyList<DateTime> NextFireTimesUtc { get; init; } = [];
}
