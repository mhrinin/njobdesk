#if NET10_0_OR_GREATER
using NJobDesk.Core.Entities;
using NJobDesk.Core.Store;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Infrastructure.Notifications;

namespace NJobDesk.Umbraco.Providers;

/// <summary>
/// Persists execution history for Umbraco's recurring background jobs from the notifications the
/// scheduling loop publishes around every run (scheduled and manually triggered alike). The
/// Executing notification precedes the runtime/server-role/MainDom checks, so skipped ticks arrive
/// as Ignored and are recorded as vetoed. The fire-instance id travels on the notification state,
/// which Umbraco copies from Executing to the completion notification.
/// </summary>
internal sealed class UmbracoJobHistoryNotificationHandler(
    IExecutionHistoryWriter historyWriter,
    TimeProvider timeProvider) :
    INotificationAsyncHandler<RecurringBackgroundJobExecutingNotification>,
    INotificationAsyncHandler<RecurringBackgroundJobExecutedNotification>,
    INotificationAsyncHandler<RecurringBackgroundJobFailedNotification>,
    INotificationAsyncHandler<RecurringBackgroundJobCanceledNotification>,
    INotificationAsyncHandler<RecurringBackgroundJobIgnoredNotification>
{
    private const string FireInstanceIdStateKey = "NJobDesk.FireInstanceId";

    public Task HandleAsync(RecurringBackgroundJobExecutingNotification notification, CancellationToken cancellationToken)
    {
        var jobId = UmbracoRecurringJobsProvider.JobId(notification.Job);
        var fireInstanceId = Guid.NewGuid().ToString("N");
        notification.State[FireInstanceIdStateKey] = fireInstanceId;

        return historyWriter.StartAsync(
            new JobExecutionHistory
            {
                FireInstanceId = fireInstanceId,
                SchedulerInstanceId = Environment.MachineName,
                ProviderKey = UmbracoRecurringJobsProvider.Key,
                JobId = jobId,
                JobName = notification.Job.GetType().Name,
                TriggerId = jobId,
                TriggerName = $"{notification.Job.GetType().Name}-schedule",
                StartedUtc = timeProvider.GetUtcNow().UtcDateTime,
                Status = ExecutionStatus.Running,
            },
            cancellationToken);
    }

    public Task HandleAsync(RecurringBackgroundJobExecutedNotification notification, CancellationToken cancellationToken) =>
        CompleteAsync(notification, ExecutionStatus.Succeeded, errorMessage: null, cancellationToken);

    public Task HandleAsync(RecurringBackgroundJobFailedNotification notification, CancellationToken cancellationToken) =>
        CompleteAsync(
            notification,
            ExecutionStatus.Failed,
            "The job threw an exception; see the Umbraco log for details.",
            cancellationToken);

    public Task HandleAsync(RecurringBackgroundJobCanceledNotification notification, CancellationToken cancellationToken) =>
        CompleteAsync(
            notification,
            ExecutionStatus.Failed,
            "The run was canceled before it finished.",
            cancellationToken);

    public Task HandleAsync(RecurringBackgroundJobIgnoredNotification notification, CancellationToken cancellationToken) =>
        CompleteAsync(
            notification,
            ExecutionStatus.Vetoed,
            "Skipped: the runtime was not ready, the server role does not run jobs, or this node is not MainDom.",
            cancellationToken);

    private Task CompleteAsync(
        RecurringBackgroundJobNotification notification,
        ExecutionStatus status,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        if (!notification.State.TryGetValue(FireInstanceIdStateKey, out var value)
            || value is not string fireInstanceId)
        {
            return Task.CompletedTask;
        }

        return historyWriter.CompleteAsync(
            UmbracoRecurringJobsProvider.Key, fireInstanceId, status, errorMessage, cancellationToken);
    }
}
#endif
