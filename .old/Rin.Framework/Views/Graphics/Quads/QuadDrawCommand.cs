using Rin.Framework.Views.Graphics.CommandHandlers;
using Rin.Framework.Views.Graphics.Commands;
using Rin.Framework.Views.Graphics.PassConfigs;

namespace Rin.Framework.Views.Graphics.Quads;

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