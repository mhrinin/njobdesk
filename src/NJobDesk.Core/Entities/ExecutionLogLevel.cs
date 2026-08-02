using System.Text.Json.Serialization;

namespace NJobDesk.Core.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExecutionLogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical,
}
