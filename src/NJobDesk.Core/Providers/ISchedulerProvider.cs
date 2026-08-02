using NJobDesk.Core.Services;

namespace NJobDesk.Core.Providers;

/// <summary>
/// A scheduler plugged into the dashboard. Provider implementations work exclusively with
/// provider-local job and trigger ids — stable, unique within the provider, and URL-safe (no
/// <c>'/'</c>); the dashboard layer prefixes them with <see cref="SchedulerProviderDescriptor.Key"/>
/// (see <see cref="CompositeId"/>) before they reach the API.
/// </summary>
public interface ISchedulerProvider
{
    /// <summary>Identity and capabilities of this provider.</summary>
    SchedulerProviderDescriptor Descriptor { get; }

    /// <summary>Read side: status, statistics, jobs, triggers, running executions.</summary>
    ISchedulerInfoService Info { get; }

    /// <summary>Write side: trigger, pause/resume, delete, reschedule.</summary>
    ISchedulerManagementService Management { get; }
}
