using Rin.Core.Graphics;
using Rin.Core.Shared.Threading;

namespace Rin.Graphics.Vulkan;

/// <summary>
///     The renderers and the hand-off between the main thread, which collects their render data, and the render
///     thread, which executes it. The hand-off is latest-wins: a batch the render thread never got to is disposed
///     rather than executed out of order.
/// </summary>
internal sealed class FrameCollector
{
    private readonly List<IRenderer> _renderers = [];
    private readonly TripleBuffer<IRenderData[]> _collected = new();

    public void Add(IRenderer renderer)
    {
        lock (_renderers)
        {
            _renderers.Add(renderer);
        }
    }

    public void Remove(IRenderer renderer)
    {
        lock (_renderers)
        {
            _renderers.Remove(renderer);
        }
    }

    public void Clear()
    {
        lock (_renderers)
        {
            _renderers.Clear();
        }
    }

    public IRenderer[] GetRenderers()
    {
        lock (_renderers)
        {
            return _renderers.ToArray();
        }
    }

    public void Collect()
    {
        var renderers = GetRenderers();
        List<IRenderData> collected = [];
        foreach (var renderer in renderers)
            if (renderer.Collect() is { } data)
                collected.Add(data);

        var discarded = _collected.Write(collected.ToArray());
        if (discarded != null) DisposeBatch(discarded);
    }

    /// <summary>
    ///     Executes the newest collected batch, if any, and says whether there was one.
    /// </summary>
    public bool Execute()
    {
        if (!_collected.TryRead(out var batch) || batch == null) return false;
        foreach (var data in batch) data.Renderer.Execute(data);
        DisposeBatch(batch);
        return true;
    }

    /// <summary>
    ///     Disposes whatever was collected but never executed. Safe after <see cref="Execute" />, because disposal
    ///     is idempotent and this is the only way to catch a batch written after the last read.
    /// </summary>
    public void DisposeRemaining()
    {
        foreach (var pending in _collected.RemainingValues)
            if (pending != null)
                DisposeBatch(pending);
    }

    // IRenderData is not IDisposable today, but a renderer may return a disposable implementation later.
    private static void DisposeBatch(IRenderData[] batch)
    {
        foreach (var data in batch)
            if (data is IDisposable disposable)
                disposable.Dispose();
    }
}
