using System.Runtime.InteropServices;
using aerox.Runtime.Widgets.Containers;
using aerox.Runtime.Widgets.Graphics.Commands;

namespace aerox.Runtime.Widgets.Graphics.Quads;

public class QuadBatch : IBatch
{
    private readonly List<Quad> _quads = [];
    public IEnumerable<ulong> GetMemoryNeeded()
    {
        return [(ulong)(_quads.Count * Marshal.SizeOf<Quad>())];
    }

    public IBatchRenderer GetRenderer() => SWidgetsModule.Get().GetBatchRenderer<QuadBatchRenderer>();

    public void AddFromCommand(BatchedCommand command)
    {
        if (command is QuadDrawCommand asQuadDraw)
        {
            _quads.AddRange(asQuadDraw.GetQuads());
        }
    }

    public IEnumerable<Quad> GetQuads() => _quads;
}