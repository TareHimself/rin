using Rin.Core.Graphics;
using Rin.Core.Shared;
using Rin.Core.Views.Graphics.CommandHandlers;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.PassConfigs;

namespace Rin.Core.Views.Graphics.Quads;

/// <summary>
///     Holds pooled copies of its quads and textures until the batch that drew it is disposed.
/// </summary>
public class QuadDrawCommand : TCommand<MainPassConfig, BatchCommandHandler>, IBatchedCommand, IDisposable
{
    private PooledMemory<Quad> _quads;
    private PooledMemory<ResourceHandle> _textures;
    private bool _disposed;

    public QuadDrawCommand(ReadOnlySpan<Quad> quads, ReadOnlySpan<ResourceHandle> textures = default)
    {
        _quads = PooledMemory<Quad>.CopyFrom(quads);
        _textures = PooledMemory<ResourceHandle>.CopyFrom(textures);
    }

    public ReadOnlySpan<Quad> Quads => _disposed ? default : _quads.AsSpan();

    public ReadOnlySpan<ResourceHandle> Textures => _disposed ? default : _textures.AsSpan();

    public IBatcher GetBatcher()
    {
        return IViewsModule.Get().GetBatcher<DefaultQuadBatcher>();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _quads.Dispose();
        _textures.Dispose();
    }
}