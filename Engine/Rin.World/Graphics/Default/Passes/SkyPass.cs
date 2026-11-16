using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Shared.Math;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Draws the view's environment behind the scene. Runs after the forward pass, so the depth test (which passes at
///     the far plane only where depth is still clear) rejects every pixel geometry covered.
/// </summary>
public partial class SkyPass(DefaultWorldViewData view) : IPass
{
    private uint _worldBufferId;

    [GraphicsShader<SkyShader>]
    private partial IGraphicsShader Shader { get; }

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        config.ReadTexture(view.DepthImageId, ImageLayout.DepthAttachment);
        config.WriteTexture(view.SceneColorImageId, ImageLayout.ColorAttachment);
        config.ReadBuffer(view.LightGridBufferId, GraphBufferUsage.Graphics);
        view.ReadEnvironment(config);
        _worldBufferId = config.CreateBuffer<WorldInfo>(GraphBufferUsage.HostThenGraphics);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var worldBuffer = graph.GetBufferOrException(_worldBufferId);
        worldBuffer.WriteSingle(view.CreateWorldInfo(graph));

        ctx
            .BeginRendering(view.Extent, [graph.GetImageOrException(view.SceneColorImageId)],
                graph.GetImageOrException(view.DepthImageId))
            .DisableFaceCulling()
            .DisableDepthWrite();

        var orientation = view.ViewTransform.Orientation;
        var tanHalfFovY = float.Tan(float.DegreesToRadians(view.FieldOfView / 2f));
        var aspect = view.Extent.Width / (float)view.Extent.Height;
        Shader.Bind(ctx)?
            .Push(new SkyShader.PushConstants
            {
                Scene = worldBuffer.ToDeviceSpan<WorldInfo>(),
                Forward = orientation.GetForward(),
                Right = orientation.GetRight() * (tanHalfFovY * aspect),
                Up = orientation.GetUp() * tanHalfFovY
            })
            .Draw(ScreenTriangle.VertexCount);

        ctx.EndRendering();
    }
}
