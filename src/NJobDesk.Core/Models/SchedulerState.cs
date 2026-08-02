using System.Text.Json.Serialization;

namespace NJobDesk.Core.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SchedulerState
{
    NotConfigured,
    Stopped,
    Started,
    Standby,
    Shutdown,
}
