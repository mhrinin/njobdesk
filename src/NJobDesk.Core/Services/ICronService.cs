using NJobDesk.Core.Models;

namespace NJobDesk.Core.Services;

/// <summary>
/// Validates cron expressions and previews upcoming fire times. The default implementation is
/// scheduler-agnostic (<see cref="CronosCronService"/>); provider packages may replace it with
/// their engine's exact semantics (e.g. Quartz's <c>CronExpression</c>).
/// </summary>
public interface ICronService
{
    /// <summary>Upper bound for the requested number of next fire times.</summary>
    const int MaxNextFireTimeCount = 20;

    /// <summary>Validates a cron expression and computes its next fire times.</summary>
    /// <param name="cronExpression">The cron expression (5 or 6 fields; seconds optional).</param>
    /// <param name="nextFireTimeCount">How many upcoming fire times to preview.</param>
    /// <param name="timeZoneId">Time zone the expression is evaluated in; local when <c>null</c>.</param>
    CronValidationResultModel Validate(string cronExpression, int nextFireTimeCount = 5, string? timeZoneId = null);
}
