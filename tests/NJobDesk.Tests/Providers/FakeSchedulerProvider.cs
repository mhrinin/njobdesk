using NJobDesk.Core.Providers;
using NJobDesk.Core.Services;
using NSubstitute;

namespace NJobDesk.Tests.Providers;

internal sealed class FakeSchedulerProvider : ISchedulerProvider
{
    public FakeSchedulerProvider(string key, SchedulerCapabilities? capabilities = null)
    {
        Descriptor = new SchedulerProviderDescriptor
        {
            Key = key,
            DisplayName = $"{key} scheduler",
            Capabilities = capabilities ?? SchedulerCapabilities.Full,
        };
        Info = Substitute.For<ISchedulerInfoService>();
        Management = Substitute.For<ISchedulerManagementService>();
    }

    public SchedulerProviderDescriptor Descriptor { get; }

    public ISchedulerInfoService Info { get; }

    public ISchedulerManagementService Management { get; }
}
