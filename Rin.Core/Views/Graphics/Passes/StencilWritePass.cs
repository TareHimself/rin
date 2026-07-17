using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;

namespace Rin.Core.Views.Graphics.Passes;

public partial class StencilWritePass : IPass
{
    private readonly StencilClip[] _clips;
    private readonly uint _mask;
    
    [GraphicsShader("Core/Shaders/Views/stencil_batch.slang")]
    private partial IGraphicsShader StencilShader {
        get;
    }
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
    public Action? OnPrune => null;

    public void Configure(IGraphConfig config)
    {
        config.WriteTexture(StencilImageId, ImageLayout.StencilAttachment);
        _clipsBufferId = config.CreateBuffer<StencilClip>(_clips.Length, GraphBufferUsage.HostThenGraphics);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        if (StencilShader.Bind(ctx) is { } bindContext)
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

    [NoReorder]
    private struct PushConstants
    {
        public required Matrix4x4 Projection;
        public required ulong ClipsBufferAddress;
    }
}