using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;

namespace Rin.Engine.Views.Graphics.Passes;

public class StencilWritePass : IPass
{
    private readonly StencilClip[] _clips;
    private readonly uint _mask;
    private readonly SurfacePassContext _surfaceContext;

    private readonly IShader _stencilShader = SGraphicsModule.Get()
        .MakeGraphics("Engine/Shaders/Views/stencil_batch.slang");

    private uint _clipsBufferId;

    public StencilWritePass(SurfacePassContext surfaceContext, uint mask, StencilClip[] clips)
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
        if (_stencilShader.Bind(ctx))
        {
            var stencilImage = graph.GetImageOrException(StencilImageId);
            var clipsBuffer = graph.GetBufferOrException(_clipsBufferId);
            clipsBuffer.WriteArray(_clips);
            ctx
                .BeginRendering(_surfaceContext.Extent, [], stencilAttachment: stencilImage)
                .DisableFaceCulling()
                .StencilWriteOnly()
                .SetStencilWriteMask(_mask);
            _stencilShader.Push(ctx, clipsBuffer.GetAddress());
            ctx.Draw(6, (uint)_clips.Length)
                .EndRendering();
        }
    }
}