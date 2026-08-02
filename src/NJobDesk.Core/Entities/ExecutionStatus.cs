using System.Text.Json.Serialization;

namespace NJobDesk.Core.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExecutionStatus
{
    Running,
    Succeeded,
    Failed,
    Vetoed,
}
