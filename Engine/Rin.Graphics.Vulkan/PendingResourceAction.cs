namespace Rin.Graphics.Vulkan;

/// <summary>
///     Work queued against one or two resources that runs on the GPU in enqueue order per resource.
/// </summary>
internal abstract class PendingResourceAction : IDisposable
{
    private bool _disposed;

    public List<TaskCompletionSource> Tasks { get; } = [];

    public void Complete()
    {
        foreach (var task in Tasks) task.TrySetResult();
        Tasks.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Release();
        foreach (var task in Tasks) task.TrySetCanceled();
        Tasks.Clear();
    }

    public void AdoptTasks(PendingResourceAction other)
    {
        Tasks.AddRange(other.Tasks);
        other.Tasks.Clear();
    }

    protected virtual void Release()
    {
    }
}
