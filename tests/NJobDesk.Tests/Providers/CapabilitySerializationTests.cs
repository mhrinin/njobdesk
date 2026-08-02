using System.Text.Json;
using NJobDesk.Core.Models;
using NJobDesk.Core.Providers;

namespace NJobDesk.Tests.Providers;

public class CapabilitySerializationTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Dashboard_status_serializes_capabilities_as_booleans()
    {
        var status = new DashboardStatusModel
        {
            ReadOnly = true,
            Providers =
            [
                new ProviderStatusModel
                {
                    Key = "alpha",
                    DisplayName = "Alpha",
                    Capabilities = SchedulerCapabilities.Full with { ScheduleEditing = false },
                    Degraded = true,
                    Error = "backend down",
                },
            ],
        };

        var json = JsonSerializer.Serialize(status, WebOptions);
        var roundTripped = JsonSerializer.Deserialize<DashboardStatusModel>(json, WebOptions)!;

        Assert.Contains("\"triggerNow\":true", json);
        Assert.Contains("\"scheduleEditing\":false", json);
        var provider = Assert.Single(roundTripped.Providers);
        Assert.True(provider.Capabilities.TriggerNow);
        Assert.False(provider.Capabilities.ScheduleEditing);
        Assert.True(provider.Degraded);
    }

    [Fact]
    public void Job_summaries_carry_their_capabilities()
    {
        var job = new JobSummaryModel
        {
            Id = "alpha:cleanup",
            ProviderKey = "alpha",
            Name = "cleanup",
            State = JobState.Normal,
            Capabilities = SchedulerCapabilities.None,
        };

        var json = JsonSerializer.Serialize(job, WebOptions);

        Assert.Contains("\"capabilities\":{", json);
        Assert.Contains("\"triggerNow\":false", json);
    }
}
