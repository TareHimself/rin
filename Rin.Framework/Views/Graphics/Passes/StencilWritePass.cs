using System.Numerics;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.FrameGraph;
using Rin.Framework.Graphics.Shaders;

namespace Rin.Framework.Views.Graphics.Passes;

public class StencilWritePass : IPass
{
    private readonly StencilClip[] _clips;
    private readonly uint _mask;

    private readonly IGraphicsShader _stencilShader = SGraphicsModule.Get()
        .MakeGraphics("Framework/Shaders/Views/stencil_batch.slang");

    private readonly SurfaceContext _surfaceContext;

    private uint _clipsBufferId;

    public StencilWritePass(SurfaceContext surfaceContext, uint mask, StencilClip[] clips)
    {
        _surfaceContext = surfaceContext;
        _mask = mask;
        _clips = clips;
    }

    private uint StencilImageId => _surfaceContext.StencilImageId;
    public uint Id { get; set; }
    public bool IsTerminal => false;

    public void Configure(IGraphConfig config)
    {
        config.WriteImage(StencilImageId, ImageLayout.StencilAttachment);
        _clipsBufferId = config.CreateBuffer<StencilClip>(_clips.Length, GraphBufferUsage.HostThenGraphics);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        if (_stencilShader.Bind(ctx) is { } bindContext)
        {
            var stencilImage = graph.GetImageOrException(StencilImageId);
            var clipsBuffer = graph.GetBufferOrException(_clipsBufferId);
            clipsBuffer.Write(_clips);
            ctx
                .BeginRendering(_surfaceContext.Extent, [], stencilAttachment: stencilImage)
                .DisableFaceCulling()
                .StencilWriteOnly()
                .SetStencilWriteMask(_mask);

            bindContext.Push(new PushConstants
                {
                    Projection = _surfaceContext.ProjectionMatrix,
                    ClipsBufferAddress = clipsBuffer.GetAddress()
                })
                .Draw(6, (uint)_clips.Length);


            ctx.EndRendering();
        }
    }

    private struct PushConstants
    {
        public required Matrix4x4 Projection;
        public required ulong ClipsBufferAddress;
    }
}