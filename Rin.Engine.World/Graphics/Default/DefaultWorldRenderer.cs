using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.World.Components;
using Rin.Engine.World.Graphics.Default.Passes;

namespace Rin.Engine.World.Graphics.Default;

public class DefaultWorldRenderer : IWorldRenderer
{
    public IWorldRenderContext Collect(IGraphBuilder builder, CameraComponent view, in Extent2D extent)
    {
        var context = new DefaultWorldRenderContext(view, extent);

        builder.AddPass(new InitWorldPass(context));
        
        if (context.HasSkinnedMeshes)
        {
            builder.AddPass(new SkinningPass(context));
            builder.AddPass(new BoundsUpdatePass(context));
        }

        // Main Pass
        {
            var cullingPass = new CullingPass(context);
            builder.AddPass(cullingPass);
            builder.AddPass(new FillIndirectBuffersPass(cullingPass, context));
        }

        builder.AddPass(new DepthPrepassIndirectPass(context));
        builder.AddPass(new FillGBufferIndirectPass(context));

        builder.AddPass(new LightingPass(context));

        return context;
    }

    public void Dispose()
    {
    }
}