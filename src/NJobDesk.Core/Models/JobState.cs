using System.Text.Json.Serialization;

namespace NJobDesk.Core.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobState
{
    None,
    Normal,
    Paused,
    Blocked,
    Error,
    Complete,
}
