using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared.Threading;

namespace Rin.Graphics.Vulkan;

/// <summary>
///     The registered renderers and the hand-off between the main thread, which collects their render data, and
///     the render thread, which renders one batch into one graph. The hand-off is latest-wins: a batch the render
///     thread never got to is disposed rather than rendered out of order. Window renderers are also indexed by their
///     window.
/// </summary>
internal sealed class FrameCollector
{
    private readonly List<IRenderer> _renderers = [];
    private readonly Dictionary<IWindow, IWindowRenderer> _windows = [];
    private readonly TripleBuffer<IRenderData[]> _collected = new();

    public void Add(IRenderer renderer)
    {
        lock (_renderers)
        {
            _renderers.Add(renderer);
            if (renderer is IWindowRenderer windowRenderer) _windows.Add(windowRenderer.GetWindow(), windowRenderer);
        }
    }

    public void Remove(IRenderer renderer)
    {
        lock (_renderers)
        {
            _renderers.Remove(renderer);
            if (renderer is IWindowRenderer windowRenderer) _windows.Remove(windowRenderer.GetWindow());
        }
    }

    public IRenderer[] GetRenderers()
    {
        lock (_renderers)
        {
            return _renderers.ToArray();
        }
    }

    public IWindowRenderer? GetWindowRenderer(IWindow window)
    {
        lock (_renderers)
        {
            return _windows.GetValueOrDefault(window);
        }
    }

    public IWindowRenderer[] GetWindowRenderers()
    {
        lock (_renderers)
        {
            return _windows.Values.ToArray();
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
    ///     Moves the newest collected batch into <paramref name="into" /> and says whether there was one. Data of a
    ///     renderer removed since it was collected is disposed instead of handed over.
    /// </summary>
    public bool TryTake(List<IRenderData> into)
    {
        if (!_collected.TryRead(out var batch) || batch == null) return false;
        lock (_renderers)
        {
            foreach (var data in batch)
                if (_renderers.Contains(data.Renderer)) into.Add(data);
                else Dispose(data);
        }

        return true;
    }

    /// <summary>
    ///     Disposes whatever was collected but never taken. Safe after <see cref="TryTake" />, because disposal is
    ///     idempotent and this is the only way to catch a batch written after the last read.
    /// </summary>
    public void DisposeRemaining()
    {
        foreach (var pending in _collected.RemainingValues)
            if (pending != null)
                DisposeBatch(pending);
    }

    public static void DisposeBatch(IReadOnlyList<IRenderData> batch)
    {
        for (var i = 0; i < batch.Count; i++) Dispose(batch[i]);
    }

    // IRenderData is not IDisposable today, but a renderer may return a disposable implementation later.
    private static void Dispose(IRenderData data)
    {
        if (data is IDisposable disposable) disposable.Dispose();
    }
}
