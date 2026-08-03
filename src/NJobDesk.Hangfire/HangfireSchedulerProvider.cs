using Hangfire;
using NJobDesk.Core.Providers;
using NJobDesk.Core.Services;

namespace NJobDesk.Hangfire;

/// <summary>
/// NJobDesk provider over Hangfire's recurring jobs: lists them with their cron schedules and
/// last/next runs (read through <see cref="JobStorage"/>, so any Hangfire storage works), and
/// triggers or removes them via <see cref="IRecurringJobManager"/>. Register with
/// <c>AddProvider&lt;HangfireSchedulerProvider&gt;()</c> after Hangfire itself is configured.
/// History is last-execution only: each job carries its most recent run, but runs are not archived
/// into the NJobDesk execution history.
/// </summary>
public sealed class HangfireSchedulerProvider : ISchedulerProvider
{
    /// <summary>The provider key job and execution ids are prefixed with.</summary>
    public const string Key = "hangfire";

    public HangfireSchedulerProvider(
        JobStorage jobStorage,
        IRecurringJobManager recurringJobManager,
        TimeProvider timeProvider)
    {
        Descriptor = new SchedulerProviderDescriptor
        {
            Key = Key,
            DisplayName = "Hangfire",
            ProviderVersion = typeof(JobStorage).Assembly.GetName().Version?.ToString(3),
            Capabilities = DefaultCapabilities,
        };
        Info = new HangfireInfoService(jobStorage, timeProvider);
        Management = new HangfireManagementService(jobStorage, recurringJobManager);
    }

    internal static SchedulerCapabilities DefaultCapabilities { get; } = new()
    {
        TriggerNow = true,
        Delete = true,
        Triggers = true,
    };

    public SchedulerProviderDescriptor Descriptor { get; }

    public ISchedulerInfoService Info { get; }

    public ISchedulerManagementService Management { get; }
}
