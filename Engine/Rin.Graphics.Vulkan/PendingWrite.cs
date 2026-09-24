using Rin.Core.Shared;

namespace Rin.Graphics.Vulkan;

internal abstract class PendingWrite(PooledMemory<byte> data) : IDisposable
{
    private const ulong StagingAlignment = 16;

    private bool _disposed;

    public PooledMemory<byte> Data { get; } = data;
    public List<TaskCompletionSource> Tasks { get; } = [];

    public ulong StagingSize => ((ulong)Data.Count + StagingAlignment - 1) & ~(StagingAlignment - 1);

    public void Complete()
    {
        foreach (var task in Tasks) task.TrySetResult();
        Tasks.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Data.Dispose();
        foreach (var task in Tasks) task.TrySetCanceled();
        Tasks.Clear();
    }

    public void AdoptTasks(PendingWrite other)
    {
        Tasks.AddRange(other.Tasks);
        other.Tasks.Clear();
    }
}
