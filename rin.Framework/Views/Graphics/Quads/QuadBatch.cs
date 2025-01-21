using System.Runtime.InteropServices;
using rin.Framework.Views.Graphics.Commands;
using rin.Framework.Views.Composite;

namespace rin.Framework.Views.Graphics.Quads;

public class QuadBatch : IBatch
{
    private readonly List<Quad> _quads = [];
    public unsafe IEnumerable<int> GetMemoryNeeded()
    {
        return [_quads.Count * sizeof(Quad)];
    }

    public IBatcher GetRenderer() => SViewsModule.Get().GetBatchRenderer<DefaultQuadBatcher>();

    public void AddFromCommand(BatchedCommand command)
    {
        if (command is QuadDrawCommand asQuadDraw)
        {
            _quads.AddRange(asQuadDraw.GetQuads());
        }
    }

    public IEnumerable<Quad> GetQuads() => _quads;
}