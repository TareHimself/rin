using Rin.Core.Views.Graphics.CommandHandlers;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.PassConfigs;

namespace Rin.Core.Views.Graphics.Quads;

public class QuadDrawCommand : TCommand<MainPassConfig, BatchCommandHandler>, IBatchedCommand
{
    private readonly List<Quad> _quads = [];

    public QuadDrawCommand(IEnumerable<Quad> quads)
    {
        _quads.AddRange(quads);
    }

    public IBatcher GetBatcher()
    {
        return IViewsModule.Get().GetBatcher<DefaultQuadBatcher>();
    }

    public IEnumerable<Quad> GetQuads()
    {
        return _quads;
    }
}