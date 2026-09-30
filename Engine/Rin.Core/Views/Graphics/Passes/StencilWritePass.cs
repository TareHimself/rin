using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Views.Graphics.Shaders;
using Rin.Shade;

namespace Rin.Core.Views.Graphics.Passes;

public partial class StencilWritePass : IPass
{
    private readonly StencilClip[] _clips;
    private readonly uint _mask;
    
    [GraphicsShader<StencilBatchShader>]
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
            clipsBuffer.Write(_clips.AsSpan());
            ctx
                .BeginRendering(_surfaceContext.Extent, [], stencilAttachment: stencilImage)
                .DisableFaceCulling()
                .StencilWriteOnly()
                .SetStencilWriteMask(_mask);

            bindContext.Push(new StencilBatchShader.PushConstants
                {
                    Projection = _surfaceContext.ProjectionMatrix,
                    Clips = new BufferRef<StencilClip>(clipsBuffer.GetAddress())
                })
                .Draw(6, (uint)_clips.Length);


            ctx.EndRendering();
        }
    }
}