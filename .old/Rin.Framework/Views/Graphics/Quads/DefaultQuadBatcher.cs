using System.Diagnostics;
using System.Numerics;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Shaders;

namespace Rin.Framework.Views.Graphics.Quads;

[ViewsBatcher]
public sealed class DefaultQuadBatcher : SimpleQuadBatcher<QuadBatch>
{
    
    private readonly IGraphicsShader _batchShader = IGraphicsModule.Get()
        .MakeGraphics("Framework/Shaders/Views/batch.slang");

    protected override IGraphicsShader GetShader()
    {
        return _batchShader;
    }

    protected override QuadBatch MakeNewBatch()
    {
        return new QuadBatch();
    }

    protected override uint WriteBatch(ViewsFrame frame, in DeviceBufferView view, QuadBatch batch,
        IGraphicsBindContext bindContext)
    {
        Debug.Assert(view.IsValid);
        var quads = batch.GetQuads().ToArray();
        if (quads.Length == 0) return 0;
        view.Write(quads);
        bindContext.Push(new Push
        {
            Projection = frame.ProjectionMatrix,
            Viewport = new Vector4(0, 0, frame.Extent.Width, frame.Extent.Height),
            Buffer = view.GetAddress()
        });
        return (uint)quads.Length;
    }

    private struct Push
    {
        public Matrix4x4 Projection;
        public Vector4 Viewport;
        public ulong Buffer;
    }
}