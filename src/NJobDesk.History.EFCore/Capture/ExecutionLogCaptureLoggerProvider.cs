using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NJobDesk.Core.Entities;
using NJobDesk.History.EFCore.Configuration;

namespace NJobDesk.History.EFCore.Capture;

internal sealed class ExecutionLogCaptureLoggerProvider(
    IOptionsMonitor<NJobDeskHistoryOptions> options,
    TimeProvider timeProvider)
    : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new CaptureLogger(categoryName, options, timeProvider);

    public void Dispose()
    {
    }

    private sealed class CaptureLogger(
        string categoryName,
        IOptionsMonitor<NJobDeskHistoryOptions> options,
        TimeProvider timeProvider)
        : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel is not LogLevel.None && ExecutionLogBuffer.Current is not null;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel is LogLevel.None || ExecutionLogBuffer.Current is not { } buffer)
            {
                return;
            }

            var level = MapLevel(logLevel);
            if (level < options.CurrentValue.Logs.MinimumLevel)
            {
                return;
            }

            buffer.Add(new JobExecutionLog
            {
                TimestampUtc = timeProvider.GetUtcNow().UtcDateTime,
                Level = level,
                Category = categoryName,
                Message = formatter(state, exception),
                Exception = exception?.ToString(),
                Properties = SerializeProperties(state),
            });
        }

        private static ExecutionLogLevel MapLevel(LogLevel level) => level switch
        {
            LogLevel.Trace => ExecutionLogLevel.Trace,
            LogLevel.Debug => ExecutionLogLevel.Debug,
            LogLevel.Information => ExecutionLogLevel.Information,
            LogLevel.Warning => ExecutionLogLevel.Warning,
            LogLevel.Error => ExecutionLogLevel.Error,
            _ => ExecutionLogLevel.Critical,
        };

        private static string? SerializeProperties<TState>(TState state)
        {
            if (state is not IReadOnlyList<KeyValuePair<string, object?>> pairs)
            {
                return null;
            }

            Dictionary<string, object?> values = [];
            foreach (var (key, value) in pairs)
            {
                if (key != "{OriginalFormat}")
                {
                    values[key] = value;
                }
            }

            if (values.Count == 0)
            {
                return null;
            }

            try
            {
                return JsonSerializer.Serialize(values);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
