using NJobDesk.Core.Providers;
using NJobDesk.Core.Services;
using NJobDesk.Core.Store;
using NJobDesk.History.EFCore.Capture;

namespace Standalone.DemoSite.Demo;

/// <summary>The full-featured demo provider: mutable in-memory jobs with seeded history and logs.</summary>
internal sealed class DemoSchedulerProvider : ISchedulerProvider
{
    public const string Key = "demo";

    public static readonly SchedulerCapabilities JobCapabilities = SchedulerCapabilities.Full;

    public DemoSchedulerProvider(
        DemoSchedulerState state,
        ICronService cronService,
        IExecutionHistoryStore historyStore,
        IExecutionHistoryWriter historyWriter,
        IExecutionLogCapture logCapture,
        IExecutionLogStore logStore,
        ILoggerFactory loggerFactory,
        TimeProvider timeProvider)
    {
        var info = new DemoSchedulerInfoService(state, historyStore, timeProvider);
        Info = info;
        Management = new DemoSchedulerManagementService(
            state, cronService, info, historyWriter, logCapture, logStore, loggerFactory, timeProvider);
    }

    public SchedulerProviderDescriptor Descriptor { get; } = new()
    {
        Key = Key,
        DisplayName = "Demo Scheduler",
        ProviderVersion = "1.0.0-demo",
        Capabilities = JobCapabilities,
    };

    public ISchedulerInfoService Info { get; }

    public ISchedulerManagementService Management { get; }
}
