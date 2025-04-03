using Rin.Engine.Views.Graphics.Commands;

namespace Rin.Engine.Views.Graphics.Quads;

public class QuadBatch : IBatch
{
    private readonly List<Quad> _quads = [];

    public IEnumerable<ulong> GetMemoryNeeded()
    {
        return [Utils.ByteSizeOf<Quad>(_quads.Count)];
    }

    public IBatcher GetRenderer()
    {
        return SViewsModule.Get().GetBatchRenderer<DefaultQuadBatcher>();
    }

    public void AddFromCommand(BatchedCommand command)
    {
        if (command is QuadDrawCommand asQuadDraw) _quads.AddRange(asQuadDraw.GetQuads());
    }

    public IEnumerable<Quad> GetQuads()
    {
        return _quads;
    }
}