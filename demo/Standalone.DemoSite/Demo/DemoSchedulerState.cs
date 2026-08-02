using Cronos;
using NJobDesk.Core.Models;

namespace Standalone.DemoSite.Demo;

/// <summary>
/// In-memory scheduler world backing the demo: a handful of jobs with cron triggers. Management
/// operations mutate this state so every dashboard action is observable without a real scheduler;
/// execution history persists through the real EF Core history store.
/// </summary>
internal sealed class DemoSchedulerState
{
    private readonly Lock _lock = new();
    private readonly TimeProvider _timeProvider;
    private readonly List<DemoJob> _jobs = [];

    public DemoSchedulerState(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        StartedUtc = timeProvider.GetUtcNow().UtcDateTime.AddHours(-49);
        Seed();
    }

    public DateTime StartedUtc { get; }

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

    public DemoJob? FindJob(string jobId)
    {
        lock (_lock)
        {
            return _jobs.FirstOrDefault(job => job.Id == jobId);
        }
    }

    public bool RemoveJob(string jobId)
    {
        lock (_lock)
        {
            return _jobs.RemoveAll(job => job.Id == jobId) > 0;
        }
    }

    public bool SetTriggerState(string triggerId, JobState from, JobState to)
    {
        lock (_lock)
        {
            var trigger = _jobs.Select(job => job.Trigger)
                .FirstOrDefault(candidate => candidate is not null && candidate.Id == triggerId);
            if (trigger is null || (from != JobState.None && trigger.State != from))
            {
                return false;
            }

            trigger.State = to;
            return true;
        }
    }

    public bool SetJobState(string jobId, JobState to)
    {
        if (FindJob(jobId) is not { Trigger: { } trigger })
        {
            return false;
        }

        trigger.State = to;
        return true;
    }

    public bool RemoveTrigger(string triggerId)
    {
        lock (_lock)
        {
            var owner = _jobs.FirstOrDefault(job => job.Trigger is { } trigger && trigger.Id == triggerId);
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

    private void Seed()
    {
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
    public string Id => $"{Group}.{Name}";

    public required string Group { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required string JobType { get; init; }

    public bool IsSystemJob { get; init; }

    public DemoTrigger? Trigger { get; set; }
}

internal sealed class DemoTrigger
{
    public string Id => $"{Group}.{Name}";

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
