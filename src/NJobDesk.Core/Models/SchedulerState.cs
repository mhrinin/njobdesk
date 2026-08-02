using System.Text.Json.Serialization;

namespace NJobDesk.Core.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SchedulerState
{
    Stopped,
    Started,
    Standby,
    Shutdown,
}
