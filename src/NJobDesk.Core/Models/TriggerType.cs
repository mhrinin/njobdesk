using System.Text.Json.Serialization;

namespace NJobDesk.Core.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TriggerType
{
    Cron,
    Simple,
    Other,
}
