using CronExpressionDescriptor;

namespace NJobDesk.Core.Mapping;

/// <summary>Human-readable cron summaries ("Every 5 minutes"), tolerant of invalid input.</summary>
public static class CronDescriptions
{
    private static readonly Options DescriptorOptions = new() { Use24HourTimeFormat = true };

    /// <summary>Describes a cron expression in plain language, or <c>null</c> when it cannot be described.</summary>
    /// <param name="cronExpression">The cron expression.</param>
    public static string? Describe(string? cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return null;
        }

        try
        {
            return ExpressionDescriptor.GetDescription(cronExpression, DescriptorOptions);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
