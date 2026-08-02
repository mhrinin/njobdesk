using NJobDesk.Core.Entities;

namespace NJobDesk.Core.Contracts;

public record ExecutionHistoryFilter
{
    public string? ProviderKey { get; init; }

    public string? JobId { get; init; }

    public string? JobName { get; init; }

    public ExecutionStatus? Status { get; init; }

    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }

    public int Skip { get; init; }

    public int Take { get; init; } = 50;
}
