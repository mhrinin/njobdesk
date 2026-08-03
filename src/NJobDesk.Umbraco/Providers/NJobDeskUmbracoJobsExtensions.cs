using Microsoft.Extensions.DependencyInjection;
using NJobDesk.Core.Providers;
using Umbraco.Cms.Core.DependencyInjection;
#if NET10_0_OR_GREATER
using Umbraco.Cms.Infrastructure.Notifications;
#endif

namespace NJobDesk.Umbraco.Providers;

/// <summary>Registers the Umbraco recurring-jobs provider with the NJobDesk dashboard.</summary>
public static class NJobDeskUmbracoJobsExtensions
{
    /// <summary>
    /// Plugs Umbraco's native recurring background jobs into the NJobDesk dashboard: every job
    /// registered through <c>AddRecurringBackgroundJob</c> (including Umbraco's own) is listed with
    /// its interval, can be triggered on demand, and has its runs recorded in the execution history
    /// (add a history package such as NJobDesk.History.EFCore to persist them).
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    public static IUmbracoBuilder AddNJobDeskUmbracoJobs(this IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<ISchedulerProvider, UmbracoRecurringJobsProvider>();
#if NET10_0_OR_GREATER
        builder
            .AddNotificationAsyncHandler<RecurringBackgroundJobExecutingNotification, UmbracoJobHistoryNotificationHandler>()
            .AddNotificationAsyncHandler<RecurringBackgroundJobExecutedNotification, UmbracoJobHistoryNotificationHandler>()
            .AddNotificationAsyncHandler<RecurringBackgroundJobFailedNotification, UmbracoJobHistoryNotificationHandler>()
            .AddNotificationAsyncHandler<RecurringBackgroundJobCanceledNotification, UmbracoJobHistoryNotificationHandler>()
            .AddNotificationAsyncHandler<RecurringBackgroundJobIgnoredNotification, UmbracoJobHistoryNotificationHandler>();
#else
        builder.Services.AddSingleton<UmbracoJobFallbackRunner>();
#endif
        return builder;
    }
}
