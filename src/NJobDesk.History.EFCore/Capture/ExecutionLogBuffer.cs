using System.Collections.Concurrent;
using NJobDesk.Core.Entities;

namespace NJobDesk.History.EFCore.Capture;

internal sealed class ExecutionLogBuffer(int capacity)
{
    private static readonly AsyncLocal<ExecutionLogBuffer?> Ambient = new();

    private readonly ConcurrentQueue<JobExecutionLog> entries = new();

    public static ExecutionLogBuffer? Current
    {
        get => Ambient.Value;
        set => Ambient.Value = value;
    }
    private int count;
    private int droppedCount;

    public int DroppedCount => droppedCount;

    public bool IsEmpty => count == 0;

    public void Add(JobExecutionLog entry)
    {
        if (Interlocked.Increment(ref count) > capacity)
        {
            Interlocked.Decrement(ref count);
            Interlocked.Increment(ref droppedCount);
            return;
        }

        entries.Enqueue(entry);
    }

    public IReadOnlyList<JobExecutionLog> Snapshot() => [.. entries];
}
