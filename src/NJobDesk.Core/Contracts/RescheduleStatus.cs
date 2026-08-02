namespace NJobDesk.Core.Contracts;

public enum RescheduleStatus
{
    Success,
    TriggerNotFound,
    NotCronTrigger,
    InvalidCronExpression,
    InvalidTimeZone,
    NotSupported,
}
