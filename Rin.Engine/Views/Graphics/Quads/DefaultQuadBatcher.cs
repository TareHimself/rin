using System.Diagnostics;
using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Shaders;

namespace Rin.Engine.Views.Graphics.Quads;

[ViewsBatcher]
public class DefaultQuadBatcher : SimpleQuadBatcher<QuadBatch>
{
    private readonly IGraphicsShader _batchShader = SGraphicsModule.Get()
        .MakeGraphics("Engine/Shaders/Views/batch.slang");

    protected override IGraphicsShader GetShader()
    {
        return _batchShader;
    }

    protected override QuadBatch MakeNewBatch()
    {
        return new QuadBatch();
    }

    protected override uint WriteBatch(ViewsFrame frame, in DeviceBufferView view, QuadBatch batch,
        IGraphicsShader shader)
    {
        Debug.Assert(view.IsValid);
        var ctx = frame.ExecutionContext;
        var quads = batch.GetQuads().ToArray();
        if (quads.Length == 0) return 0;

        view.WriteArray(quads);
        _batchShader.Push(ctx, new Push
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