using Rin.Core.Views.Graphics.Commands;

namespace Rin.Core.Views.Graphics.Quads;

public class QuadBatch : IBatch
{
    private readonly List<Quad> _quads = [];

    public ulong GetMemoryNeeded()
    {
        return Utils.ByteSizeOf<Quad>(_quads.Count);
    }

    public IBatcher GetBatcher()
    {
        return IViewsModule.Get().GetBatcher<DefaultQuadBatcher>();
    }

    public void AddFromCommand(ICommand command)
    {
        if (command is QuadDrawCommand asQuadDraw) _quads.AddRange(asQuadDraw.GetQuads());
    }

    public List<Quad> GetQuads()
    {
        return _quads;
    }
}