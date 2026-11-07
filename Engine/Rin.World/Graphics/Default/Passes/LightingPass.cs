using Rin.Core.Extensions;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Graphics.Default.Passes;

public partial class LightingPass(DefaultWorldViewData context) : IPass
{
    [GraphicsShader<LightingShader>]
    private partial IGraphicsShader Shader { get; }

    private uint _worldBufferId;

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        config.ReadTexture(context.GBufferImage0, ImageLayout.ShaderReadOnly);
        config.ReadTexture(context.GBufferImage1, ImageLayout.ShaderReadOnly);
        config.ReadTexture(context.GBufferImage2, ImageLayout.ShaderReadOnly);
        config.ReadTexture(context.GBufferImage3, ImageLayout.ShaderReadOnly);
        context.OutputImageId = config.CreateTexture(context.Extent, LightingShader.Descriptor.Output.Format.ToImageFormat(), ImageLayout.ColorAttachment);
        _worldBufferId = config.CreateBuffer<LightingInfo>(GraphBufferUsage.HostThenGraphics);
        config.ReadBuffer(context.LightGridBufferId, GraphBufferUsage.Graphics);
        config.ReadBuffer(context.PointLightsBufferId, GraphBufferUsage.Graphics);
        config.ReadBuffer(context.DirectionalLightsBufferId, GraphBufferUsage.Graphics);
        config.ReadBuffer(context.LightBinsBufferId, GraphBufferUsage.Graphics);
        config.ReadBuffer(context.TileMasksBufferId, GraphBufferUsage.Graphics);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        if (Shader.Bind(ctx) is { } bindContext)
        {
            var gBuffer0 = graph.GetImageOrException(context.GBufferImage0);
            var gBuffer1 = graph.GetImageOrException(context.GBufferImage1);
            var gBuffer2 = graph.GetImageOrException(context.GBufferImage2);
            var gBuffer3 = graph.GetImageOrException(context.GBufferImage3);
            var outputImage = graph.GetImageOrException(context.OutputImageId);
            var buffer = graph.GetBufferOrException(_worldBufferId);
            var gridBuffer = graph.GetBufferOrException(context.LightGridBufferId);

            buffer.WriteSingle(
                new LightingInfo
                {
                    GBuffer = new GBufferHandles
                    {
                        GBuffer0 = gBuffer0,
                        GBuffer1 = gBuffer1,
                        GBuffer2 = gBuffer2,
                        GBuffer3 = gBuffer3
                    },
                    Eye = context.ViewTransform.Position,
                    Grid = new BufferRef<LightGridInfo>(gridBuffer.GetAddress())
                });

            ctx
                .BeginRendering(context.Extent, [outputImage], clearColor: Vector4.Zero)
                .DisableFaceCulling();
            bindContext
                .Push(new LightingShader.PushConstants { Data = new BufferRef<LightingInfo>(buffer.GetAddress()) })
                .Draw(6);

            ctx.EndRendering();
        }
    }
}
