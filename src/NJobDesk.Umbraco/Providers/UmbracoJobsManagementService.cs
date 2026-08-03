using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;
using NJobDesk.Core.Services;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace NJobDesk.Umbraco.Providers;

/// <summary>
/// Triggers recurring jobs: through Umbraco's native <c>IRecurringBackgroundJobTrigger&lt;TJob&gt;</c>
/// on Umbraco 17.5+ (the run executes inside Umbraco's own scheduling loop, so history and server-role
/// checks apply as if the schedule had fired), or by running the job directly on Umbraco 16 (single-flight,
/// with history recorded by the runner). Everything else is unsupported for native jobs.
/// </summary>
internal sealed class UmbracoJobsManagementService(
    IEnumerable<IRecurringBackgroundJob> jobs,
    IServiceProvider serviceProvider) : ISchedulerManagementService
{
    private const string TriggerExecutionMethodName = "TriggerExecution";

    private readonly UnsupportedSchedulerManagementService _unsupported = new();

    public Task<bool> TriggerJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var job = jobs.FirstOrDefault(candidate => UmbracoRecurringJobsProvider.JobId(candidate) == jobId);
        if (job is null || !UmbracoRecurringJobsProvider.CanTrigger(job))
        {
            return Task.FromResult(false);
        }

#if NET10_0_OR_GREATER
        var triggerType = typeof(IRecurringBackgroundJobTrigger<>).MakeGenericType(job.GetType());
        if (serviceProvider.GetService(triggerType) is not { } trigger
            || triggerType.GetMethod(TriggerExecutionMethodName, Type.EmptyTypes) is not { } triggerMethod)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult((bool)triggerMethod.Invoke(trigger, [])!);
#else
        var runner = (UmbracoJobFallbackRunner)serviceProvider.GetService(typeof(UmbracoJobFallbackRunner))!;
        return runner.TriggerAsync(job, jobId, cancellationToken);
#endif
    }

    public Task<bool> PauseJobAsync(string jobId, CancellationToken cancellationToken = default) =>
        _unsupported.PauseJobAsync(jobId, cancellationToken);

    public Task<bool> ResumeJobAsync(string jobId, CancellationToken cancellationToken = default) =>
        _unsupported.ResumeJobAsync(jobId, cancellationToken);

    public Task<bool> DeleteJobAsync(string jobId, CancellationToken cancellationToken = default) =>
        _unsupported.DeleteJobAsync(jobId, cancellationToken);

    public Task<bool> PauseTriggerAsync(string triggerId, CancellationToken cancellationToken = default) =>
        _unsupported.PauseTriggerAsync(triggerId, cancellationToken);

    public Task<bool> ResumeTriggerAsync(string triggerId, CancellationToken cancellationToken = default) =>
        _unsupported.ResumeTriggerAsync(triggerId, cancellationToken);

    public Task<bool> ResetTriggerFromErrorAsync(string triggerId, CancellationToken cancellationToken = default) =>
        _unsupported.ResetTriggerFromErrorAsync(triggerId, cancellationToken);

    public Task<bool> UnscheduleTriggerAsync(string triggerId, CancellationToken cancellationToken = default) =>
        _unsupported.UnscheduleTriggerAsync(triggerId, cancellationToken);

    public Task PauseAllAsync(CancellationToken cancellationToken = default) =>
        _unsupported.PauseAllAsync(cancellationToken);

    public Task ResumeAllAsync(CancellationToken cancellationToken = default) =>
        _unsupported.ResumeAllAsync(cancellationToken);

    public Task<RescheduleResult> RescheduleAsync(
        string triggerId,
        RescheduleRequestModel request,
        CancellationToken cancellationToken = default) =>
        _unsupported.RescheduleAsync(triggerId, request, cancellationToken);
}
