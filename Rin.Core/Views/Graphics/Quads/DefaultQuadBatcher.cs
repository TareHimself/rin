using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Shaders;

namespace Rin.Core.Views.Graphics.Quads;

[ViewsBatcher]
public sealed partial class DefaultQuadBatcher : SimpleQuadBatcher<QuadBatch>
{
    [GraphicsShader("Core/Shaders/Views/batch.slang")]
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
        
        bindContext.Push(new Push
        {
            Projection = frame.ProjectionMatrix,
            Viewport = new Vector4(0, 0, frame.Extent.Width, frame.Extent.Height),
            Buffer = view.GetAddress()
        });
        return (uint)quads.Count;
    }

    [NoReorder]
    private struct Push
    {
        public Matrix4x4 Projection;
        public Vector4 Viewport;
        public ulong Buffer;
    }
}