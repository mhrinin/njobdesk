namespace NJobDesk.Core.Providers;

/// <summary>Identifies a scheduler provider and declares what it supports.</summary>
public record SchedulerProviderDescriptor
{
    /// <summary>
    /// Stable, unique, URL-safe key (lowercase letters, digits, and dashes, e.g. <c>"quartz"</c>).
    /// Prefixes every job, trigger, and execution id the dashboard exposes.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>Human-readable name shown on provider badges and status cards.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Version of the underlying scheduler, when known.</summary>
    public string? ProviderVersion { get; init; }

    /// <summary>The dashboard features this provider supports.</summary>
    public required SchedulerCapabilities Capabilities { get; init; }
}
