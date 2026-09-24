namespace Rin.Core.Graphics.Graph;

public interface IGraphBuilder
{
    public uint AddPass(IPass pass);

    /// <summary>
    ///     Keeps the resource alive until the frame using this graph has rendered. Returns 0 if the handle was
    ///     already freed or isn't ready yet.
    /// </summary>
    public uint AddExternalImage(ResourceHandle handle, Action? onDispose = null);

    public uint AddDestinationImage(ResourceHandle handle, Action? onDispose = null);

    /// <inheritdoc cref="AddExternalImage" />
    public uint AddExternalBuffer(in DeviceBufferView view, Action? onDispose = null);

    /// <summary>
    ///     Ties <paramref name="disposable" /> to this graph's lifetime: it's disposed once the frame that ran the
    ///     graph has finished rendering, or right away if the graph ends up with nothing to run.
    /// </summary>
    public void AddDisposable(IDisposable disposable);
}