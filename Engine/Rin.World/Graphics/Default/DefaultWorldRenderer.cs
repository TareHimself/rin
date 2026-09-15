using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.World.Components;
using Rin.World.Graphics.Default.Passes;

namespace Rin.World.Graphics.Default;

public class DefaultWorldRenderer : IWorldRenderer
{
    public IWorldRenderContext Snapshot(CameraComponent view, in Extent2D extent)
    {
        return new DefaultWorldRenderContext(view, extent);
    }

    public void Build(IGraphBuilder builder, IWorldRenderContext context)
    {
        if (context is not DefaultWorldRenderContext ctx)
            throw new ArgumentException($"Expected {nameof(DefaultWorldRenderContext)}", nameof(context));

        builder.AddPass(new InitWorldPass(ctx));

        if (ctx.WillDoSkinning)
        {
            builder.AddPass(new SkinningPass(ctx));
            builder.AddPass(new BoundsUpdatePass(ctx));
        }

        // Main Pass
        {
            var cullingPass = new CullingPass(ctx);
            builder.AddPass(cullingPass);
            builder.AddPass(new FillIndirectBuffersPass(cullingPass, ctx));
        }

        builder.AddPass(new DepthPrepassIndirectPass(ctx));
        builder.AddPass(new FillGBufferIndirectPass(ctx));

        builder.AddPass(new LightingPass(ctx));
    }

    public void Dispose()
    {
    }
}
