using Microsoft.EntityFrameworkCore;
using NJobDesk.Core.Entities;
using NJobDesk.History.EFCore.Persistence;

namespace Standalone.DemoSite.Demo;

/// <summary>
/// Seeds two days of believable execution history (deterministic, ~7% failure rate) into the real
/// EF history store on first boot. Later boots keep whatever the store already holds, which is what
/// demonstrates persistence across restarts.
/// </summary>
internal sealed class DemoHistorySeeder(
    IDbContextFactory<NJobDeskHistoryDbContext> contextFactory,
    DemoSchedulerState state,
    TimeProvider timeProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await context.ExecutionHistory.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        List<(JobExecutionHistory Execution, DemoJob Job, bool Failed)> seeded = [];
        var executionSeed = 0;
        foreach (var job in state.Jobs)
        {
            var cadence = TimeSpan.FromMinutes(90 + (executionSeed % 5) * 47);
            for (var startedUtc = now.AddHours(-48); startedUtc < now.AddMinutes(-10); startedUtc += cadence)
            {
                executionSeed++;
                var failed = executionSeed % 13 == 0 || (job.Name == "newsletter-dispatch" && executionSeed % 5 == 0);
                var durationMs = 400 + (executionSeed % 23) * 350;
                var execution = NewEntry(job, startedUtc, failed ? ExecutionStatus.Failed : ExecutionStatus.Succeeded);
                execution.FinishedUtc = startedUtc.AddMilliseconds(durationMs);
                execution.DurationMs = durationMs;
                execution.ErrorMessage = failed ? "Demo failure: upstream service returned 503." : null;
                context.ExecutionHistory.Add(execution);
                seeded.Add((execution, job, failed));
            }
        }

        // One run currently in flight so the overview shows a live item on first boot.
        context.ExecutionHistory.Add(NewEntry(state.Jobs[2], now.AddMinutes(-2), ExecutionStatus.Running));
        await context.SaveChangesAsync(cancellationToken);

        foreach (var (execution, job, failed) in seeded)
        {
            foreach (var log in LogsFor(execution, job, failed))
            {
                context.ExecutionLogs.Add(log);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static IEnumerable<JobExecutionLog> LogsFor(JobExecutionHistory execution, DemoJob job, bool failed)
    {
        yield return NewLog(execution, execution.StartedUtc, ExecutionLogLevel.Information, job, "Job started.");
        yield return NewLog(
            execution,
            execution.StartedUtc.AddMilliseconds(execution.DurationMs!.Value / 2.0),
            ExecutionLogLevel.Debug,
            job,
            $"Processed batch 1 of 3 ({execution.DurationMs / 3} items).",
            properties: """{"batch":1,"total":3}""");
        if (failed)
        {
            yield return NewLog(
                execution,
                execution.FinishedUtc!.Value,
                ExecutionLogLevel.Error,
                job,
                "Request to upstream failed.",
                exception: "System.Net.Http.HttpRequestException: Response status code does not indicate success: 503 (Service Unavailable).\n   at Demo.UpstreamClient.SendAsync()");
        }

        yield return NewLog(
            execution,
            execution.FinishedUtc!.Value,
            ExecutionLogLevel.Information,
            job,
            failed ? "Job finished with errors." : "Job finished.");
    }

    private static JobExecutionHistory NewEntry(DemoJob job, DateTime startedUtc, ExecutionStatus status) => new()
    {
        FireInstanceId = $"seed-{job.Id}-{startedUtc.Ticks}",
        SchedulerInstanceId = "demo-instance",
        ProviderKey = DemoSchedulerProvider.Key,
        JobId = job.Id,
        JobName = job.Name,
        JobGroup = job.Group,
        TriggerId = job.Trigger?.Id,
        TriggerName = job.Trigger?.Name,
        StartedUtc = startedUtc,
        Status = status,
    };

    private static JobExecutionLog NewLog(
        JobExecutionHistory execution,
        DateTime timestampUtc,
        ExecutionLogLevel level,
        DemoJob job,
        string message,
        string? exception = null,
        string? properties = null) => new()
        {
            ExecutionId = execution.Id,
            TimestampUtc = timestampUtc,
            Level = level,
            Category = $"Demo.Jobs.{job.JobType}",
            Message = message,
            Exception = exception,
            Properties = properties,
        };
}
