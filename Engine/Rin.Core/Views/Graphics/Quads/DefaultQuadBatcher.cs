using System.Diagnostics;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Views.Graphics.Shaders;
using Rin.Shade;

namespace Rin.Core.Views.Graphics.Quads;

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
        if (batch.QuadCount == 0) return 0;

        ulong offset = 0;
        foreach (var command in batch.Commands) offset += view.Write(command.Quads, offset);

        bindContext.Push(new QuadBatchShader.PushConstants
        {
            Projection = frame.ProjectionMatrix,
            Viewport = new Vector4(0, 0, frame.Extent.Width, frame.Extent.Height),
            Quads = view.ToDeviceSpan<Quad>()
        });
        return (uint)batch.QuadCount;
    }
}
