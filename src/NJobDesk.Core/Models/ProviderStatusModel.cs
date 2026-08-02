using NJobDesk.Core.Providers;

namespace NJobDesk.Core.Models;

public record ProviderStatusModel
{
    public required string Key { get; init; }

    public required string DisplayName { get; init; }

    public string? ProviderVersion { get; init; }

    public required SchedulerCapabilities Capabilities { get; init; }

    public bool Degraded { get; init; }

    public string? Error { get; init; }

    public SchedulerStatusModel? Status { get; init; }
}
