using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Writes the view's output image from its linear scene colour. Everything before this pass works on scene
///     brightness, and everything after it on display values.
/// </summary>
public partial class ToneMapPass(DefaultWorldViewData view) : IPass
{
    [GraphicsShader<ToneMapShader>]
    private partial IGraphicsShader Shader { get; }

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        config.ReadTexture(view.SceneColorImageId, ImageLayout.ShaderReadOnly);
        config.WriteTexture(view.OutputImageId, ImageLayout.ColorAttachment);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        ctx
            .BeginRendering(view.Extent, [graph.GetImageOrException(view.OutputImageId)])
            .DisableFaceCulling()
            .DisableDepthTest();

        Shader.Bind(ctx)?
            .Push(new ToneMapShader.PushConstants
            {
                Scene = graph.GetImageOrException(view.SceneColorImageId),
                Curve = view.ToneMapCurve
            })
            .Draw(ScreenTriangle.VertexCount);

        ctx.EndRendering();
    }
}
