using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NJobDesk.Core.Contracts;
using NJobDesk.Core.Entities;
using NJobDesk.History.EFCore.Configuration;
using NJobDesk.History.EFCore.Maintenance;
using NJobDesk.History.EFCore.Store;
using NSubstitute;

namespace NJobDesk.Tests.History;

public sealed class ExecutionHistoryCleanupServiceTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteHistoryDatabaseFixture _database = new();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(Now));
    private readonly EfExecutionHistoryStore _store;
    private readonly EfExecutionHistoryWriter _writer;

    public ExecutionHistoryCleanupServiceTests()
    {
        _store = new EfExecutionHistoryStore(_database, _timeProvider);
        _writer = new EfExecutionHistoryWriter(_database, _timeProvider);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Cleanup_prunes_expired_runs_and_fails_day_old_running_ones()
    {
        await _writer.StartAsync(Entry("expired", ExecutionStatus.Succeeded, Now.AddDays(-31)));
        await _writer.StartAsync(Entry("kept", ExecutionStatus.Succeeded, Now.AddDays(-29)));
        await _writer.StartAsync(Entry("stuck", ExecutionStatus.Running, Now.AddHours(-25)));
        await _writer.StartAsync(Entry("live", ExecutionStatus.Running, Now.AddMinutes(-10)));

        await CreateService().RunCleanupAsync(CancellationToken.None);

        var page = await _store.GetPageAsync(new ExecutionHistoryFilter());
        Assert.Equal(["live", "stuck", "kept"], page.Items.Select(entry => entry.FireInstanceId));
        Assert.Equal(ExecutionStatus.Failed, page.Items.Single(entry => entry.FireInstanceId == "stuck").Status);
        Assert.Equal(ExecutionStatus.Running, page.Items.Single(entry => entry.FireInstanceId == "live").Status);
    }

    [Fact]
    public async Task Startup_reconciliation_fails_runs_left_running_by_a_previous_process()
    {
        await _writer.StartAsync(Entry("leftover", ExecutionStatus.Running, Now.AddMinutes(-3)));

        var service = new ExecutionHistoryReconciliationService(
            _store,
            Options.Create(new NJobDeskHistoryOptions()),
            _timeProvider,
            NullLogger<ExecutionHistoryReconciliationService>.Instance);
        await service.StartAsync(CancellationToken.None);

        var entry = Assert.Single((await _store.GetPageAsync(new ExecutionHistoryFilter())).Items);
        Assert.Equal(ExecutionStatus.Failed, entry.Status);
    }

    private ExecutionHistoryCleanupService CreateService()
    {
        var monitor = Substitute.For<IOptionsMonitor<NJobDeskHistoryOptions>>();
        monitor.CurrentValue.Returns(new NJobDeskHistoryOptions());
        return new ExecutionHistoryCleanupService(
            _store, monitor, _timeProvider, NullLogger<ExecutionHistoryCleanupService>.Instance);
    }

    private static JobExecutionHistory Entry(string fireInstanceId, ExecutionStatus status, DateTime startedUtc) => new()
    {
        FireInstanceId = fireInstanceId,
        SchedulerInstanceId = "node-1",
        ProviderKey = "demo",
        JobId = "jobs.cleanup",
        JobName = "cleanup",
        StartedUtc = startedUtc,
        Status = status,
        FinishedUtc = status is ExecutionStatus.Running ? null : startedUtc.AddSeconds(5),
    };
}
