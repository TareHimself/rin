using Rin.Core.Graphics;
using Rin.Core.Views.Graphics.CommandHandlers;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.PassConfigs;

namespace Rin.Core.Views.Graphics.Quads;

public class QuadDrawCommand : TCommand<MainPassConfig, BatchCommandHandler>, IBatchedCommand
{
    private readonly List<Quad> _quads = [];
    private readonly List<ResourceHandle> _textures = [];

    public QuadDrawCommand(ReadOnlySpan<Quad> quads, ReadOnlySpan<ResourceHandle> textures = default)
    {
        _quads.AddRange(quads);
        _textures.AddRange(textures);
    }

    public IBatcher GetBatcher()
    {
        return IViewsModule.Get().GetBatcher<DefaultQuadBatcher>();
    }

    public IReadOnlyCollection<Quad> GetQuads()
    {
        return _quads;
    }

    public IReadOnlyCollection<ResourceHandle> GetTextures()
    {
        return _textures;
    }
}