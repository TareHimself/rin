using Rin.Engine.Views.Graphics.CommandHandlers;
using Rin.Engine.Views.Graphics.Commands;
using Rin.Engine.Views.Graphics.PassConfigs;
using Rin.Engine.Views.Graphics.Passes;

namespace Rin.Engine.Views.Graphics.Quads;

public class QuadDrawCommand : TCommand<MainPassConfig,BatchCommandHandler>, IBatchedCommand
{
    private readonly List<Quad> _quads = [];

    public QuadDrawCommand(IEnumerable<Quad> quads)
    {
        _quads.AddRange(quads);
    }

    public IBatcher GetBatcher()
    {
        return SViewsModule.Get().GetBatcher<DefaultQuadBatcher>();
    }

    public IEnumerable<Quad> GetQuads()
    {
        return _quads;
    }
}