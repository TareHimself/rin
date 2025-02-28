using Rin.Engine.Views.Graphics.Commands;

namespace Rin.Engine.Views.Graphics.Quads;

public class QuadDrawCommand : BatchedCommand
{
    private readonly List<Quad> _quads = [];

    public QuadDrawCommand(IEnumerable<Quad> quads)
    {
        _quads.AddRange(quads);
    }

    public override IBatcher GetBatchRenderer()
    {
        return SViewsModule.Get().GetBatchRenderer<DefaultQuadBatcher>();
    }

    public IEnumerable<Quad> GetQuads()
    {
        return _quads;
    }
}