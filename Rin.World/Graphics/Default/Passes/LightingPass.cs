using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;

namespace Rin.World.Graphics.Default.Passes;

public class LightingPass(DefaultWorldRenderContext context) : IPass
{
    private readonly IGraphicsShader _shader = IGraphicsModule.Get()
        .MakeGraphics("World/Shaders/lighting.slang");

    private uint _lightBufferId;
    private uint _worldBufferId;

    public uint Id { get; set; }
    public bool IsTerminal => false;
    public Action? OnPrune => null;

    public void Configure(IGraphConfig config)
    {
        config.ReadTexture(context.GBufferImage0, ImageLayout.ShaderReadOnly);
        config.ReadTexture(context.GBufferImage1, ImageLayout.ShaderReadOnly);
        config.ReadTexture(context.GBufferImage2, ImageLayout.ShaderReadOnly);
        context.OutputImageId = config.CreateTexture(context.Extent, ImageFormat.RGBA32, ImageLayout.ColorAttachment);
        _worldBufferId = config.CreateBuffer<LightingInfo>(GraphBufferUsage.HostThenGraphics);
        _lightBufferId = config.CreateBuffer<LightInfo>(context.Lights.Length, GraphBufferUsage.HostThenGraphics);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        if (_shader.Bind(ctx) is { } bindContext)
        {
            var gBuffer0 = graph.GetImageOrException(context.GBufferImage0);
            var gBuffer1 = graph.GetImageOrException(context.GBufferImage1);
            var gBuffer2 = graph.GetImageOrException(context.GBufferImage2);
            var outputImage = graph.GetImageOrException(context.OutputImageId);
            var buffer = graph.GetBufferOrException(_worldBufferId);
            var lightsBuffer = graph.GetBufferOrException(_lightBufferId);
            lightsBuffer.Write(context.Lights);

            buffer.Write(
                new LightingInfo
                {
                    GBuffer0 = gBuffer0,
                    GBuffer1 = gBuffer1,
                    GBuffer2 = gBuffer2,
                    EyeLocation = context.ViewTransform.Position,
                    LightsBuffer = lightsBuffer.GetAddress(),
                    NumLights = context.Lights.Length
                });

            ctx
                .BeginRendering(context.Extent, [outputImage], clearColor: Vector4.Zero)
                .DisableFaceCulling();
            bindContext
                .Push(buffer.GetAddress())
                .Draw(6);

            ctx.EndRendering();
        }
    }

    private struct LightingInfo
    {
        public required ResourceHandle GBuffer0;
        public required ResourceHandle GBuffer1;
        public required ResourceHandle GBuffer2;
        public required Vector3 EyeLocation;
        public required ulong LightsBuffer;
        public required int NumLights;
    }
}