using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.CommandHandlers;
using Rin.Core.Views.Graphics.Commands;

namespace experiment.StencilAndCover.Rendering;

[StructLayout(LayoutKind.Sequential)]
internal struct StencilFillPush
{
    public required Matrix4x4 Projection;
    public required Matrix4x4 Transform;
    public required ulong VerticesAddress;
}

// Pass 1 of stencil-and-cover: accumulates a per-pixel winding count into a dedicated stencil
// attachment (not the view-clip system's) by drawing each contour's triangle fan with
// front-face INCREMENT_AND_WRAP / back-face DECREMENT_AND_WRAP. No color is written.
public class StencilFillCommand : TCommand<StencilFillPassConfig, StencilFillCommandHandler>
{
    // Local-space triangle list (3 vertices per triangle), one fan per contour concatenated.
    public required List<Vector2> FanVertices;
    public required Matrix4x4 Transform;

    // Populated by StencilFillCommandHandler.Configure; read by CoverCommandHandler afterward.
    public uint StencilImageId;
    internal uint VertexBufferId;
}

// Rendering scope for this pass is opened/closed per-command inside the handler (each command
// owns its own stencil attachment), so this pass config itself does nothing — same pattern as
// BlurPassConfig.
public class StencilFillPassConfig : IPassConfig
{
    public void Init(SurfaceContext surfaceContext) { }
    public void Configure(IGraphConfig config) { }
    public void Begin(ICompiledGraph graph, IExecutionContext ctx) { }
    public void End(ICompiledGraph graph, IExecutionContext ctx) { }
}

public class StencilFillCommandHandler : ICommandHandler
{
    private readonly IGraphicsShader _shader =
        IGraphicsModule.Get().MakeGraphics("StencilAndCover/stencil_fill.slang");

    private StencilFillCommand[] _commands = [];

    public void Init(ICommand[] commands)
    {
        _commands = commands.Cast<StencilFillCommand>().ToArray();
    }

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
        foreach (var command in _commands)
        {
            command.StencilImageId =
                config.CreateTexture(surfaceContext.Extent, ImageFormat.Stencil, ImageLayout.StencilAttachment);
            command.VertexBufferId =
                config.CreateBuffer<Vector2>(Math.Max(1, command.FanVertices.Count), GraphBufferUsage.HostThenGraphics);
        }
    }

    public void Execute(IPassConfig passConfig, SurfaceContext surfaceContext, ICompiledGraph graph,
        IExecutionContext ctx)
    {
        foreach (var command in _commands)
        {
            if (command.FanVertices.Count == 0) continue;

            var stencilImage = graph.GetImageOrException(command.StencilImageId);
            var vertexBuffer = graph.GetBufferOrException(command.VertexBufferId);
            vertexBuffer.Write(CollectionsMarshal.AsSpan(command.FanVertices));

            ctx.ClearStencilImages(0, [stencilImage]);
            ctx.BeginRendering(surfaceContext.Extent, [], stencilAttachment: stencilImage)
                .DisableFaceCulling()
                .StencilFillOp();

            if (_shader.Bind(ctx) is { } bind)
                bind.Push(new StencilFillPush
                    {
                        Projection = surfaceContext.ProjectionMatrix,
                        Transform = command.Transform,
                        VerticesAddress = vertexBuffer.GetAddress()
                    })
                    .Draw((uint)command.FanVertices.Count);

            ctx.EndRendering();
        }
    }
}
