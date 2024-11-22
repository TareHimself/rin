using System.Runtime.InteropServices;
using rin.Widgets.Containers;
using rin.Widgets.Graphics.Commands;

namespace rin.Widgets.Graphics.Quads;

public class QuadBatch : IBatch
{
    private readonly List<Quad> _quads = [];
    public IEnumerable<ulong> GetMemoryNeeded()
    {
        return [(ulong)(_quads.Count * Marshal.SizeOf<Quad>())];
    }

    public IBatcher GetRenderer() => SWidgetsModule.Get().GetBatchRenderer<DefaultQuadBatcher>();

    public void AddFromCommand(BatchedCommand command)
    {
        if (command is QuadDrawCommand asQuadDraw)
        {
            _quads.AddRange(asQuadDraw.GetQuads());
        }
    }

    public IEnumerable<Quad> GetQuads() => _quads;
}