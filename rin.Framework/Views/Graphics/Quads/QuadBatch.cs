using System.Runtime.InteropServices;
using rin.Framework.Core;
using rin.Framework.Views.Graphics.Commands;
using rin.Framework.Views.Composite;

namespace rin.Framework.Views.Graphics.Quads;

public class QuadBatch : IBatch
{
    private readonly List<Quad> _quads = [];
    public IEnumerable<ulong> GetMemoryNeeded()
    {
        return [Utils.ByteSizeOf<Quad>(_quads.Count)];
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