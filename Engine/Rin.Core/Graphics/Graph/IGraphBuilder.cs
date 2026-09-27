namespace Rin.Core.Graphics.Graph;

public interface IGraphBuilder
{
    public uint AddPass(IPass pass);

    /// <summary>
    /// Add an external resource to the graph, must be called for all external resources that will be used in <see cref="IPass.Execute"/>
    /// </summary>
    public uint AddExternalImage(ResourceHandle handle, Action? onDispose = null);

    /// <inheritdoc cref="AddExternalImage" />
    public uint AddExternalBuffer(in DeviceBufferView view, Action? onDispose = null);

    /// <summary>
    ///     Ties <paramref name="disposable" /> to this graph's lifetime: it's disposed once the frame that ran the
    ///     graph has finished rendering, or right away if the graph ends up with nothing to run.
    /// </summary>
    public void AddDisposable(IDisposable disposable);

    /// <summary>
    ///     The object stored under <paramref name="key" /> for this build, created on first request, so several
    ///     contributors to one graph can share passes and resources. An <see cref="IDisposable" /> entry is
    ///     disposed like <see cref="AddDisposable" />.
    /// </summary>
    public T GetOrAddShared<T>(object key, Func<IGraphBuilder, T> create) where T : class;
}