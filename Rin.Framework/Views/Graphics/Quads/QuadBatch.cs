using Rin.Framework.Views.Graphics.Commands;

namespace Rin.Framework.Views.Graphics.Quads;

public class QuadBatch : IBatch
{
    private readonly List<Quad> _quads = [];

    public IEnumerable<ulong> GetMemoryNeeded()
    {
        return [Utils.ByteSizeOf<Quad>(_quads.Count)];
    }

    public IBatcher GetBatcher()
    {
        return SViewsModule.Get().GetBatcher<DefaultQuadBatcher>();
    }

    public void AddFromCommand(ICommand command)
    {
        if (command is QuadDrawCommand asQuadDraw) _quads.AddRange(asQuadDraw.GetQuads());
    }

    public IEnumerable<Quad> GetQuads()
    {
        return _quads;
    }
}