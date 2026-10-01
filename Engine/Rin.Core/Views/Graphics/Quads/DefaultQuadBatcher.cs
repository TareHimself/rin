using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Views.Graphics.Shaders;
using Rin.Shade;

namespace Rin.Core.Views.Graphics.Quads;

[ViewsBatcher]
public sealed partial class DefaultQuadBatcher : SimpleQuadBatcher<QuadBatch>
{
    [GraphicsShader<QuadBatchShader>]
    private partial IGraphicsShader BatchShader { get; }

    protected override IGraphicsShader GetShader()
    {
        return BatchShader;
    }

    protected override QuadBatch MakeNewBatch()
    {
        return new QuadBatch();
    }

    protected override uint WriteBatch(ViewsFrame frame, in DeviceBufferView view, QuadBatch batch,
        IGraphicsBindContext bindContext)
    {
        Debug.Assert(view.IsValid);
        var quads = batch.GetQuads();
        if (quads.Count == 0) return 0;
        unsafe
        {
            fixed (Quad* data = CollectionsMarshal.AsSpan(quads))
            {
                view.Write(new ReadOnlySpan<Quad>(data, quads.Count));
            }
        }
        
        bindContext.Push(new QuadBatchShader.PushConstants
        {
            Projection = frame.ProjectionMatrix,
            Viewport = new Vector4(0, 0, frame.Extent.Width, frame.Extent.Height),
            Quads = new BufferRef<Quad>(view.GetAddress())
        });
        return (uint)quads.Count;
    }
}
