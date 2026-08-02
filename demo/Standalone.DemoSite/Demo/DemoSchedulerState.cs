using Cronos;
using NJobDesk.Core.Entities;
using NJobDesk.Core.Models;

namespace Standalone.DemoSite.Demo;

/// <summary>
/// In-memory scheduler world backing the demo: a handful of jobs with cron triggers and two days
/// of seeded execution history. Management operations mutate this state so every dashboard action
/// is observable without a real scheduler.
/// </summary>
internal sealed class DemoSchedulerState
{
    private const string DemoInstanceId = "demo-instance";

    private readonly Lock _lock = new();
    private readonly TimeProvider _timeProvider;
    private readonly List<DemoJob> _jobs = [];
    private readonly List<JobExecutionHistory> _history = [];
    private readonly Dictionary<long, List<JobExecutionLog>> _logs = [];
    private long _nextExecutionId = 1;
    private long _nextLogId = 1;

    public DemoSchedulerState(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        Seed();
    }

    public DateTime StartedUtc { get; private set; }

    public bool Paused { get; private set; }

    public IReadOnlyList<DemoJob> Jobs
    {
        get
        {
            lock (_lock)
            {
                return [.. _jobs];
            }
        }
    }

    public DemoJob? FindJob(string group, string name)
    {
        lock (_lock)
        {
            return _jobs.FirstOrDefault(job => job.Group == group && job.Name == name);
        }
    }

    public bool RemoveJob(string group, string name)
    {
        lock (_lock)
        {
            return _jobs.RemoveAll(job => job.Group == group && job.Name == name) > 0;
        }
    }

    public bool SetTriggerState(string group, string name, JobState from, JobState to)
    {
        lock (_lock)
        {
            var trigger = _jobs.Select(job => job.Trigger)
                .FirstOrDefault(t => t is not null && t.Group == group && t.Name == name);
            if (trigger is null || (from != JobState.None && trigger.State != from))
            {
                return false;
            }

            trigger.State = to;
            return true;
        }
    }

    public bool SetJobState(string group, string name, JobState to)
    {
        if (FindJob(group, name) is not { Trigger: { } trigger })
        {
            return false;
        }

        trigger.State = to;
        return true;
    }

    public bool RemoveTrigger(string group, string name)
    {
        lock (_lock)
        {
            var owner = _jobs.FirstOrDefault(job => job.Trigger is { } t && t.Group == group && t.Name == name);
            if (owner is null)
            {
                return false;
            }

            owner.Trigger = null;
            return true;
        }
    }

    public void SetAllTriggers(JobState state)
    {
        lock (_lock)
        {
            Paused = state == JobState.Paused;
            foreach (var trigger in _jobs.Select(job => job.Trigger))
            {
                if (trigger is not null && trigger.State != JobState.Error)
                {
                    trigger.State = state;
                }
            }
        }
    }

    public long StartRun(DemoJob job)
    {
        lock (_lock)
        {
            var execution = new JobExecutionHistory
            {
                Id = _nextExecutionId++,
                FireInstanceId = Guid.NewGuid().ToString("N"),
                SchedulerInstanceId = DemoInstanceId,
                JobGroup = job.Group,
                JobName = job.Name,
                TriggerGroup = job.Trigger?.Group ?? job.Group,
                TriggerName = job.Trigger?.Name ?? $"{job.Name}-trigger",
                StartedUtc = _timeProvider.GetUtcNow().UtcDateTime,
                Status = ExecutionStatus.Running,
            };
            _history.Add(execution);
            _logs[execution.Id] = [NewLog(execution.Id, ExecutionLogLevel.Information, job.JobType, "Job started (manual trigger).")];
            return execution.Id;
        }
    }

    public void CompleteRun(long executionId, bool failed)
    {
        lock (_lock)
        {
            var execution = _history.FirstOrDefault(entry => entry.Id == executionId);
            if (execution is null || execution.Status != ExecutionStatus.Running)
            {
                return;
            }

            var finished = _timeProvider.GetUtcNow().UtcDateTime;
            execution.FinishedUtc = finished;
            execution.DurationMs = (long)(finished - execution.StartedUtc).TotalMilliseconds;
            execution.Status = failed ? ExecutionStatus.Failed : ExecutionStatus.Succeeded;
            execution.ErrorMessage = failed ? "Demo failure: upstream service returned 503." : null;

            var logs = _logs[executionId];
            if (failed)
            {
                logs.Add(NewLog(executionId, ExecutionLogLevel.Error, execution.JobName,
                    "Request to upstream failed.",
                    "System.Net.Http.HttpRequestException: Response status code does not indicate success: 503 (Service Unavailable).\n   at Demo.UpstreamClient.SendAsync()"));
            }

            logs.Add(NewLog(executionId, ExecutionLogLevel.Information, execution.JobName,
                failed ? "Job finished with errors." : "Job finished."));
        }
    }

    public (IReadOnlyList<JobExecutionHistory> Items, Dictionary<long, List<JobExecutionLog>> Logs) SnapshotHistory()
    {
        lock (_lock)
        {
            return ([.. _history], _logs.ToDictionary(pair => pair.Key, pair => pair.Value.ToList()));
        }
    }

    public int RemoveFinishedBefore(DateTime cutoffUtc, int batchSize)
    {
        lock (_lock)
        {
            var expired = _history
                .Where(entry => entry.Status != ExecutionStatus.Running && entry.FinishedUtc < cutoffUtc)
                .Take(batchSize)
                .ToList();
            foreach (var entry in expired)
            {
                _history.Remove(entry);
                _logs.Remove(entry.Id);
            }

            return expired.Count;
        }
    }

    public int MarkStaleRunning(DateTime startedBeforeUtc, string reason)
    {
        lock (_lock)
        {
            var stale = _history
                .Where(entry => entry.Status == ExecutionStatus.Running && entry.StartedUtc < startedBeforeUtc)
                .ToList();
            foreach (var entry in stale)
            {
                entry.Status = ExecutionStatus.Failed;
                entry.ErrorMessage = reason;
            }

            return stale.Count;
        }
    }

    private JobExecutionLog NewLog(long executionId, ExecutionLogLevel level, string category, string message, string? exception = null) => new()
    {
        Id = _nextLogId++,
        ExecutionId = executionId,
        TimestampUtc = _timeProvider.GetUtcNow().UtcDateTime,
        Level = level,
        Category = $"Demo.Jobs.{category}",
        Message = message,
        Exception = exception,
    };

    private void Seed()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        StartedUtc = now.AddHours(-49);

        _jobs.AddRange(
        [
            NewJob("reports", "daily-sales-report", "Aggregates yesterday's orders into the sales report.", "0 0 6 * * ?"),
            NewJob("reports", "weekly-digest", "Sends the weekly activity digest to subscribers.", "0 30 7 ? * MON"),
            NewJob("sync", "crm-contact-sync", "Pulls changed contacts from the CRM.", "0 */15 * * * ?"),
            NewJob("sync", "warehouse-stock-sync", "Refreshes stock levels from the warehouse API.", "0 5/10 * * * ?"),
            NewJob("maintenance", "orphaned-media-cleanup", "Removes media items with no references.", "0 0 3 * * ?"),
            NewJob("maintenance", "history-retention", "Prunes dashboard execution history.", "0 0 4 * * ?", isSystemJob: true),
            NewJob("default", "cache-warmup", "Re-primes the output cache after deployments.", "0 0/30 * * * ?"),
            NewJob("default", "newsletter-dispatch", "Sends queued newsletters in batches.", "0 10 9 * * ?"),
        ]);

        _jobs[3].Trigger!.State = JobState.Paused;
        _jobs[7].Trigger!.State = JobState.Error;

        // Two days of believable history: every job ran on a spread-out cadence with a ~7% failure
        // rate (deterministic — no randomness so restarts look identical).
        var executionSeed = 0;
        foreach (var job in _jobs)
        {
            var cadence = TimeSpan.FromMinutes(90 + (executionSeed % 5) * 47);
            for (var startedUtc = now.AddHours(-48); startedUtc < now.AddMinutes(-10); startedUtc += cadence)
            {
                executionSeed++;
                var failed = executionSeed % 13 == 0 || (job.Name == "newsletter-dispatch" && executionSeed % 5 == 0);
                var durationMs = 400 + (executionSeed % 23) * 350;
                AddSeededExecution(job, startedUtc, durationMs, failed);
            }
        }

        // One run currently in flight so the overview shows a live item.
        var running = _jobs[2];
        StartRun(running);
    }

    private void AddSeededExecution(DemoJob job, DateTime startedUtc, int durationMs, bool failed)
    {
        var execution = new JobExecutionHistory
        {
            Id = _nextExecutionId++,
            FireInstanceId = Guid.NewGuid().ToString("N"),
            SchedulerInstanceId = DemoInstanceId,
            JobGroup = job.Group,
            JobName = job.Name,
            TriggerGroup = job.Trigger!.Group,
            TriggerName = job.Trigger.Name,
            StartedUtc = startedUtc,
            FinishedUtc = startedUtc.AddMilliseconds(durationMs),
            DurationMs = durationMs,
            Status = failed ? ExecutionStatus.Failed : ExecutionStatus.Succeeded,
            ErrorMessage = failed ? "Demo failure: upstream service returned 503." : null,
        };
        _history.Add(execution);

        List<JobExecutionLog> logs =
        [
            new()
            {
                Id = _nextLogId++,
                ExecutionId = execution.Id,
                TimestampUtc = startedUtc,
                Level = ExecutionLogLevel.Information,
                Category = $"Demo.Jobs.{job.JobType}",
                Message = "Job started.",
            },
            new()
            {
                Id = _nextLogId++,
                ExecutionId = execution.Id,
                TimestampUtc = startedUtc.AddMilliseconds(durationMs / 2.0),
                Level = ExecutionLogLevel.Debug,
                Category = $"Demo.Jobs.{job.JobType}",
                Message = $"Processed batch 1 of 3 ({durationMs / 3} items).",
                Properties = """{"batch":1,"total":3}""",
            },
        ];

        if (failed)
        {
            logs.Add(new JobExecutionLog
            {
                Id = _nextLogId++,
                ExecutionId = execution.Id,
                TimestampUtc = execution.FinishedUtc!.Value,
                Level = ExecutionLogLevel.Error,
                Category = $"Demo.Jobs.{job.JobType}",
                Message = "Request to upstream failed.",
                Exception = "System.Net.Http.HttpRequestException: Response status code does not indicate success: 503 (Service Unavailable).\n   at Demo.UpstreamClient.SendAsync()",
            });
        }

        _logs[execution.Id] = logs;
    }

    private DemoJob NewJob(string group, string name, string description, string cron, bool isSystemJob = false) => new()
    {
        Group = group,
        Name = name,
        Description = description,
        JobType = string.Concat(name.Split('-').Select(part => char.ToUpperInvariant(part[0]) + part[1..])),
        IsSystemJob = isSystemJob,
        Trigger = new DemoTrigger
        {
            Group = group,
            Name = $"{name}-trigger",
            CronExpression = cron,
            StartTimeUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-30),
        },
    };
}

internal sealed class DemoJob
{
    public required string Group { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required string JobType { get; init; }

    public bool IsSystemJob { get; init; }

    public DemoTrigger? Trigger { get; set; }
}

internal sealed class DemoTrigger
{
    public required string Group { get; init; }

    public required string Name { get; init; }

    public required string CronExpression { get; set; }

    public string? TimeZoneId { get; set; }

    public JobState State { get; set; } = JobState.Normal;

    public DateTime StartTimeUtc { get; init; }

    public DateTime? NextFireTimeUtc(TimeProvider timeProvider)
    {
        try
        {
            var fields = CronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            var parsed = Cronos.CronExpression.Parse(
                CronExpression, fields == 6 ? CronFormat.IncludeSeconds : CronFormat.Standard);
            return parsed.GetNextOccurrence(timeProvider.GetUtcNow(), TimeZoneInfo.Utc)?.UtcDateTime;
        }
        catch (CronFormatException)
        {
            return null;
        }
    }
}
