using Microsoft.Extensions.DependencyInjection;
using NJobDesk.Core.Providers;
using NJobDesk.Core.Services;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace NJobDesk.Umbraco.Providers;

/// <summary>
/// NJobDesk provider over Umbraco's native recurring background jobs
/// (<see cref="IRecurringBackgroundJob"/>): lists every registered job with its interval as a
/// read-only synthetic trigger, records run history (through the recurring-job notifications on
/// Umbraco 17.5+, or for manually triggered runs on Umbraco 16), and triggers jobs on demand.
/// Register with <c>AddNJobDeskUmbracoJobs()</c> on the Umbraco builder.
/// </summary>
public sealed class UmbracoRecurringJobsProvider : ISchedulerProvider
{
    /// <summary>The provider key job and execution ids are prefixed with.</summary>
    public const string Key = "umbraco";

    public UmbracoRecurringJobsProvider(IServiceProvider serviceProvider)
    {
        var version = serviceProvider.GetRequiredService<IUmbracoVersion>().SemanticVersion.ToString();
        Descriptor = new SchedulerProviderDescriptor
        {
            Key = Key,
            DisplayName = "Umbraco recurring jobs",
            ProviderVersion = version,
            Capabilities = DefaultCapabilities,
        };
        Info = ActivatorUtilities.CreateInstance<UmbracoJobsInfoService>(serviceProvider);
        Management = ActivatorUtilities.CreateInstance<UmbracoJobsManagementService>(serviceProvider);
    }

    internal static SchedulerCapabilities DefaultCapabilities { get; } = new()
    {
        TriggerNow = true,
        History = true,
    };

    public SchedulerProviderDescriptor Descriptor { get; }

    public ISchedulerInfoService Info { get; }

    public ISchedulerManagementService Management { get; }

    internal static string JobId(IRecurringBackgroundJob job) => job.GetType().FullName ?? job.GetType().Name;

    internal static bool CanTrigger(IRecurringBackgroundJob job) =>
#if NET10_0_OR_GREATER
        job is ITriggerableRecurringBackgroundJob;
#else
        true;
#endif
}
