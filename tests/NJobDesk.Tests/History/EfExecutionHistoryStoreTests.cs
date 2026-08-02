using Microsoft.Extensions.Time.Testing;
using NJobDesk.Core.Contracts;
using NJobDesk.Core.Entities;
using NJobDesk.History.EFCore.Store;

namespace NJobDesk.Tests.History;

public sealed class EfExecutionHistoryStoreTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteHistoryDatabaseFixture _database = new();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(Now));
    private readonly EfExecutionHistoryStore _store;
    private readonly EfExecutionHistoryWriter _writer;

    public EfExecutionHistoryStoreTests()
    {
        _store = new EfExecutionHistoryStore(_database, _timeProvider);
        _writer = new EfExecutionHistoryWriter(_database, _timeProvider);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Writer_round_trips_a_run_through_start_and_complete()
    {
        await _writer.StartAsync(Entry("demo", "fire-1", "cleanup", startedUtc: Now.AddSeconds(-90)));

        _timeProvider.SetUtcNow(new DateTimeOffset(Now.AddSeconds(30)));
        await _writer.CompleteAsync("demo", "fire-1", ExecutionStatus.Failed, "boom");

        var page = await _store.GetPageAsync(new ExecutionHistoryFilter());
        var entry = Assert.Single(page.Items);
        Assert.Equal(ExecutionStatus.Failed, entry.Status);
        Assert.Equal("boom", entry.ErrorMessage);
        Assert.Equal(Now.AddSeconds(30), entry.FinishedUtc);
        Assert.Equal(120_000, entry.DurationMs);
    }

    [Fact]
    public async Task Complete_ignores_unknown_runs_and_other_providers()
    {
        await _writer.StartAsync(Entry("demo", "fire-1", "cleanup"));

        await _writer.CompleteAsync("other", "fire-1", ExecutionStatus.Succeeded);

        var running = await _store.GetRunningAsync();
        Assert.Single(running);
    }

    [Fact]
    public async Task Page_filters_by_provider_job_id_name_status_and_window()
    {
        await _writer.StartAsync(Entry("demo", "fire-1", "cleanup", ExecutionStatus.Succeeded, Now.AddHours(-2)));
        await _writer.StartAsync(Entry("demo", "fire-2", "cleanup", ExecutionStatus.Failed, Now.AddHours(-1)));
        await _writer.StartAsync(Entry("demo", "fire-3", "sync-contacts", ExecutionStatus.Succeeded, Now.AddHours(-1)));
        await _writer.StartAsync(Entry("basic", "fire-1", "cleanup", ExecutionStatus.Succeeded, Now.AddHours(-1)));

        Assert.Equal(3, (await _store.GetPageAsync(new ExecutionHistoryFilter { ProviderKey = "demo" })).Total);
        Assert.Equal(2, (await _store.GetPageAsync(new ExecutionHistoryFilter { ProviderKey = "demo", JobId = "jobs.cleanup" })).Total);
        Assert.Equal(1, (await _store.GetPageAsync(new ExecutionHistoryFilter { JobName = "contact" })).Total);
        Assert.Equal(1, (await _store.GetPageAsync(new ExecutionHistoryFilter { Status = ExecutionStatus.Failed })).Total);
        Assert.Equal(3, (await _store.GetPageAsync(new ExecutionHistoryFilter { FromUtc = Now.AddMinutes(-90) })).Total);
        Assert.Equal(1, (await _store.GetPageAsync(new ExecutionHistoryFilter { ToUtc = Now.AddMinutes(-90) })).Total);
    }

    [Fact]
    public async Task Page_orders_newest_first_and_pages()
    {
        for (var index = 0; index < 5; index++)
        {
            await _writer.StartAsync(Entry("demo", $"fire-{index}", "cleanup", ExecutionStatus.Succeeded, Now.AddMinutes(-index)));
        }

        var page = await _store.GetPageAsync(new ExecutionHistoryFilter { Skip = 1, Take = 2 });

        Assert.Equal(5, page.Total);
        Assert.Equal(["fire-1", "fire-2"], page.Items.Select(entry => entry.FireInstanceId));
    }

    [Fact]
    public async Task Statistics_bucket_the_last_24_hours()
    {
        await _writer.StartAsync(Entry("demo", "fire-1", "cleanup", ExecutionStatus.Succeeded, Now.AddHours(-1)));
        await _writer.StartAsync(Entry("demo", "fire-2", "cleanup", ExecutionStatus.Succeeded, Now.AddHours(-1)));
        await _writer.StartAsync(Entry("demo", "fire-3", "cleanup", ExecutionStatus.Failed, Now.AddHours(-2)));
        await _writer.StartAsync(Entry("demo", "fire-4", "cleanup", ExecutionStatus.Succeeded, Now.AddHours(-30)));
        await _writer.StartAsync(Entry("demo", "fire-5", "cleanup"));

        var statistics = await _store.GetStatisticsAsync();

        Assert.Equal(1, statistics.RunningCount);
        Assert.Equal(2, statistics.Succeeded24h);
        Assert.Equal(1, statistics.Failed24h);
        Assert.Equal(24, statistics.Buckets.Count);
        Assert.Equal(2, statistics.Buckets.Single(bucket => bucket.HourStartUtc == Now.AddHours(-1)).Succeeded);
        Assert.Equal(1, statistics.Buckets.Single(bucket => bucket.HourStartUtc == Now.AddHours(-2)).Failed);
    }

    [Fact]
    public async Task Retention_deletes_finished_runs_in_batches_and_keeps_running_ones()
    {
        for (var index = 0; index < 5; index++)
        {
            await _writer.StartAsync(Entry("demo", $"old-{index}", "cleanup", ExecutionStatus.Succeeded, Now.AddDays(-40)));
        }

        await _writer.StartAsync(Entry("demo", "old-running", "cleanup", startedUtc: Now.AddDays(-40)));
        await _writer.StartAsync(Entry("demo", "recent", "cleanup", ExecutionStatus.Succeeded, Now.AddDays(-1)));

        var deleted = await _store.DeleteFinishedBeforeAsync(Now.AddDays(-30), batchSize: 2);

        Assert.Equal(5, deleted);
        var remaining = await _store.GetPageAsync(new ExecutionHistoryFilter());
        Assert.Equal(["recent", "old-running"], remaining.Items.Select(entry => entry.FireInstanceId));
    }

    [Fact]
    public async Task Stale_running_runs_are_failed_with_optional_node_prefix()
    {
        await _writer.StartAsync(Entry("demo", "mine", "cleanup", startedUtc: Now.AddHours(-30), instanceId: "NODE-A/1"));
        await _writer.StartAsync(Entry("demo", "theirs", "cleanup", startedUtc: Now.AddHours(-30), instanceId: "NODE-B/1"));
        await _writer.StartAsync(Entry("demo", "fresh", "cleanup", startedUtc: Now.AddMinutes(-5), instanceId: "NODE-A/1"));

        var reconciled = await _store.MarkStaleRunningAsFailedAsync(Now.AddHours(-24), "NODE-A", "stale");

        Assert.Equal(1, reconciled);
        var failed = Assert.Single((await _store.GetPageAsync(new ExecutionHistoryFilter { Status = ExecutionStatus.Failed })).Items);
        Assert.Equal("mine", failed.FireInstanceId);
        Assert.Equal("stale", failed.ErrorMessage);
        Assert.Equal(Now, failed.FinishedUtc);
    }

    private static JobExecutionHistory Entry(
        string providerKey,
        string fireInstanceId,
        string jobName,
        ExecutionStatus status = ExecutionStatus.Running,
        DateTime? startedUtc = null,
        string instanceId = "node-1") => new()
        {
            FireInstanceId = fireInstanceId,
            SchedulerInstanceId = instanceId,
            ProviderKey = providerKey,
            JobId = $"jobs.{jobName}",
            JobName = jobName,
            JobGroup = "jobs",
            TriggerId = $"jobs.{jobName}-trigger",
            TriggerName = $"{jobName}-trigger",
            StartedUtc = startedUtc ?? Now,
            Status = status,
            FinishedUtc = status is ExecutionStatus.Running ? null : (startedUtc ?? Now).AddSeconds(5),
        };
}
