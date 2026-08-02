using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NJobDesk.Core.Entities;
using NJobDesk.History.EFCore.Capture;
using NJobDesk.History.EFCore.Configuration;
using NJobDesk.History.EFCore.Store;
using NSubstitute;

namespace NJobDesk.Tests.History;

public sealed class ExecutionLogCaptureTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteHistoryDatabaseFixture _database = new();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(Now));
    private readonly NJobDeskHistoryOptions _options = new();
    private readonly ExecutionLogCapture _capture;
    private readonly ExecutionLogCaptureLoggerProvider _loggerProvider;
    private readonly ExecutionLogStore _logStore;
    private readonly EfExecutionHistoryWriter _writer;

    public ExecutionLogCaptureTests()
    {
        var monitor = Substitute.For<IOptionsMonitor<NJobDeskHistoryOptions>>();
        monitor.CurrentValue.Returns(_ => _options);
        _capture = new ExecutionLogCapture(monitor, _timeProvider);
        _loggerProvider = new ExecutionLogCaptureLoggerProvider(monitor, _timeProvider);
        _logStore = new ExecutionLogStore(_database, _timeProvider, NullLogger<ExecutionLogStore>.Instance);
        _writer = new EfExecutionHistoryWriter(_database, _timeProvider);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Captured_logs_attach_to_the_run_by_provider_and_fire_instance()
    {
        await StartRunAsync("demo", "fire-1");
        var logger = _loggerProvider.CreateLogger("Demo.Jobs.Cleanup");

        using (var scope = _capture.BeginScope())
        {
            Assert.NotNull(scope);
            logger.LogInformation("Deleting {Count} items.", 3);
            logger.LogDebug("Filtered below minimum level.");
            await _logStore.SaveAsync("demo", "fire-1", scope);
        }

        var logs = await ReadLogsAsync("demo", "fire-1");
        var entry = Assert.Single(logs);
        Assert.Equal("Deleting 3 items.", entry.Message);
        Assert.Equal(ExecutionLogLevel.Information, entry.Level);
        Assert.Equal("Demo.Jobs.Cleanup", entry.Category);
        Assert.Contains("\"Count\":3", entry.Properties);
    }

    [Fact]
    public async Task Recorded_exceptions_and_overflow_are_persisted()
    {
        _options.Logs.MaxEntriesPerRun = 2;
        await StartRunAsync("demo", "fire-1");
        var logger = _loggerProvider.CreateLogger("Demo.Jobs.Cleanup");

        using (var scope = _capture.BeginScope())
        {
            logger.LogInformation("first");
            scope!.RecordException("Demo.Jobs.Cleanup", new InvalidOperationException("boom"));
            logger.LogInformation("dropped by the capacity limit");
            await _logStore.SaveAsync("demo", "fire-1", scope);
        }

        var logs = await ReadLogsAsync("demo", "fire-1");
        Assert.Equal(3, logs.Count);
        Assert.Contains(logs, entry => entry.Exception?.Contains("boom") == true);
        Assert.Contains(logs, entry => entry.Message.Contains("Log capture limit reached; 1 additional entries"));
    }

    [Fact]
    public async Task Logs_for_unknown_runs_are_discarded_without_throwing()
    {
        var logger = _loggerProvider.CreateLogger("Demo.Jobs.Cleanup");

        using var scope = _capture.BeginScope();
        logger.LogInformation("orphan");
        await _logStore.SaveAsync("demo", "missing", scope!);
    }

    [Fact]
    public void Logging_outside_a_scope_captures_nothing()
    {
        var logger = _loggerProvider.CreateLogger("Demo.Jobs.Cleanup");

        Assert.False(logger.IsEnabled(LogLevel.Information));
    }

    [Fact]
    public void Capture_is_disabled_by_options()
    {
        _options.Logs.Enabled = false;

        Assert.Null(_capture.BeginScope());
    }

    private async Task StartRunAsync(string providerKey, string fireInstanceId) =>
        await _writer.StartAsync(new JobExecutionHistory
        {
            FireInstanceId = fireInstanceId,
            SchedulerInstanceId = "node-1",
            ProviderKey = providerKey,
            JobId = "jobs.cleanup",
            JobName = "cleanup",
            StartedUtc = Now,
            Status = ExecutionStatus.Running,
        });

    private async Task<IReadOnlyList<JobExecutionLog>> ReadLogsAsync(string providerKey, string fireInstanceId)
    {
        await using var context = _database.CreateDbContext();
        var executionId = context.ExecutionHistory
            .Single(entry => entry.ProviderKey == providerKey && entry.FireInstanceId == fireInstanceId).Id;
        return [.. context.ExecutionLogs.Where(entry => entry.ExecutionId == executionId).OrderBy(entry => entry.Id)];
    }
}
