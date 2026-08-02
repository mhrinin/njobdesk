using NJobDesk.Core.Models;

namespace NJobDesk.Core.Contracts;

public record RescheduleResult(RescheduleStatus Status, string? Error = null, TriggerModel? Trigger = null)
{
    public static RescheduleResult Success(TriggerModel trigger) => new(RescheduleStatus.Success, Trigger: trigger);
}
