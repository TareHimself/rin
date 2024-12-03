using rin.Framework.Widgets.Graphics.Commands;

namespace rin.Framework.Widgets.Graphics.Quads;

public class QuadDrawCommand : BatchedCommand
{
    private readonly List<Quad> _quads = [];

    public QuadDrawCommand(IEnumerable<Quad> quads)
    {
        _quads.AddRange(quads);
    }
    
    public override IBatcher GetBatchRenderer() => SWidgetsModule.Get().GetBatchRenderer<DefaultQuadBatcher>();

    public IEnumerable<Quad> GetQuads() => _quads;
}