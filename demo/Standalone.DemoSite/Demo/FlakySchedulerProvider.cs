using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;
using NJobDesk.Core.Providers;
using NJobDesk.Core.Services;

namespace Standalone.DemoSite.Demo;

/// <summary>
/// A provider whose scheduler is permanently unreachable. Demonstrates per-provider fault
/// isolation: the dashboard stays functional and shows this provider as degraded.
/// </summary>
internal sealed class FlakySchedulerProvider : ISchedulerProvider
{
    public SchedulerProviderDescriptor Descriptor { get; } = new()
    {
        Key = "flaky",
        DisplayName = "Flaky Scheduler",
        Capabilities = SchedulerCapabilities.Full,
    };

    public ISchedulerInfoService Info { get; } = new ThrowingInfoService();

    public ISchedulerManagementService Management { get; } = new UnsupportedSchedulerManagementService();

    private static InvalidOperationException Outage() =>
        new("The scheduler backend did not respond (demo outage).");

    private sealed class ThrowingInfoService : ISchedulerInfoService
    {
        public Task<SchedulerStatusModel> GetStatusAsync(CancellationToken cancellationToken = default) =>
            throw Outage();

        public Task<SchedulerStatisticsModel> GetStatisticsAsync(CancellationToken cancellationToken = default) =>
            throw Outage();

        public Task<PagedResult<JobSummaryModel>> GetJobsAsync(
            int skip, int take, string? group = null, string? filter = null, CancellationToken cancellationToken = default) =>
            throw Outage();

        public Task<JobDetailModel?> GetJobAsync(string jobId, CancellationToken cancellationToken = default) =>
            throw Outage();

        public Task<TriggerModel?> GetTriggerAsync(string triggerId, CancellationToken cancellationToken = default) =>
            throw Outage();

        public Task<IReadOnlyList<ExecutionModel>> GetRunningAsync(CancellationToken cancellationToken = default) =>
            throw Outage();
    }
}
