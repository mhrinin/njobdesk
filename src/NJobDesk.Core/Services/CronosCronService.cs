using Cronos;
using NJobDesk.Core.Mapping;
using NJobDesk.Core.Models;

namespace NJobDesk.Core.Services;

/// <summary>
/// Scheduler-agnostic <see cref="ICronService"/> built on Cronos. Accepts standard 5-field and
/// seconds-including 6-field expressions (Quartz-style <c>?</c>, <c>L</c>, <c>W</c> and <c>#</c>
/// tokens included). Seven-field Quartz expressions (with a year) are rejected — provider packages
/// with engine-exact cron semantics can replace this service.
/// </summary>
public sealed class CronosCronService(TimeProvider timeProvider) : ICronService
{
    /// <inheritdoc />
    public CronValidationResultModel Validate(string cronExpression, int nextFireTimeCount = 5, string? timeZoneId = null)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return new CronValidationResultModel { IsValid = false, Error = "The cron expression is empty." };
        }

        TimeZoneInfo timeZone;
        try
        {
            timeZone = timeZoneId is null ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return new CronValidationResultModel { IsValid = false, Error = $"Unknown time zone '{timeZoneId}'." };
        }

        var fieldCount = cronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (fieldCount > 6)
        {
            return new CronValidationResultModel
            {
                IsValid = false,
                Error = "Expressions with a year field are not supported; use 5 fields, or 6 including seconds.",
            };
        }

        CronExpression parsed;
        try
        {
            parsed = CronExpression.Parse(cronExpression, fieldCount == 6 ? CronFormat.IncludeSeconds : CronFormat.Standard);
        }
        catch (CronFormatException exception)
        {
            return new CronValidationResultModel { IsValid = false, Error = exception.Message };
        }

        return new CronValidationResultModel
        {
            IsValid = true,
            Summary = CronDescriptions.Describe(cronExpression) ?? cronExpression,
            NextFireTimesUtc = ComputeNextFireTimes(parsed, timeZone, Math.Clamp(nextFireTimeCount, 0, ICronService.MaxNextFireTimeCount)),
        };
    }

    private List<DateTime> ComputeNextFireTimes(CronExpression expression, TimeZoneInfo timeZone, int count)
    {
        List<DateTime> fireTimes = [];
        var after = timeProvider.GetUtcNow();
        while (fireTimes.Count < count && expression.GetNextOccurrence(after, timeZone) is { } next)
        {
            fireTimes.Add(next.UtcDateTime);
            after = next;
        }

        return fireTimes;
    }
}
