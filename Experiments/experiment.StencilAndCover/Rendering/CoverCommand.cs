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
internal struct CoverPush
{
    public required Matrix4x4 Projection;
    public required Vector2 MinPos;
    public required Vector2 MaxPos;
    public required Vector4 Color;
}

// One boundary edge's true (unflattened) curve, in screen space, plus the thin *oriented* quad
// boundary_aa.slang rasterizes it against — must match struct EdgeInstance in that shader exactly.
// The quad hugs the curve's chord (not an axis-aligned box around it), so a long diagonal edge
// doesn't cover a huge swath of the shape's interior and overlap unrelated neighboring edges.
[StructLayout(LayoutKind.Sequential)]
public struct EdgeInstance
{
    public required Vector2 P0;
    public required Vector2 Control;
    public required Vector2 P2;
    public required Vector2 Corner0;
    public required Vector2 Corner1;
    public required Vector2 Corner2;
    public required Vector2 Corner3;
    public required Vector4 Color;
}

[StructLayout(LayoutKind.Sequential)]
internal struct BoundaryPush
{
    public required Matrix4x4 Projection;
    public required ulong EdgesAddress;
}

// Pass 2 of stencil-and-cover: draws the shape's bounding quad gated by the stencil test built
// up by StencilFillCommand (nonzero winding = inside), giving a correctly filled but
// polygon-faceted shape, then overlays thin per-edge quads that correct the boundary against the
// true curves using an analytic signed-distance AA ramp.
public class CoverCommand(StencilFillCommand fillCommand) : TCommand<CoverPassConfig, CoverCommandHandler>
{
    public readonly StencilFillCommand FillCommand = fillCommand;
    public required Vector2 BoundsMin;
    public required Vector2 BoundsMax;
    public required List<EdgeInstance> Edges;
    public required Vector4 Color;

    internal uint EdgeBufferId;
}

// Rendering scope is opened/closed per-command inside CoverCommandHandler (each command binds a
// different StencilFillCommand's stencil attachment, so it can't be MainPassConfig — that type
// actively owns its own fixed BeginRendering/EndRendering pair for the view-clip stencil image,
// and nesting another BeginRendering inside it corrupts the command buffer).
public class CoverPassConfig : IPassConfig
{
    public void Init(SurfaceContext surfaceContext) { }
    public void Configure(IGraphConfig config) { }
    public void Begin(ICompiledGraph graph, IExecutionContext ctx) { }
    public void End(ICompiledGraph graph, IExecutionContext ctx) { }
}

public class CoverCommandHandler : ICommandHandler
{
    private readonly IGraphicsShader _coverShader =
        IGraphicsModule.Get().MakeGraphics("StencilAndCover/cover_fill.slang");

    private readonly IGraphicsShader _boundaryShader =
        IGraphicsModule.Get().MakeGraphics("StencilAndCover/boundary_aa.slang");

    private CoverCommand[] _commands = [];

    public void Init(ICommand[] commands)
    {
        _commands = commands.Cast<CoverCommand>().ToArray();
    }

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
        config.WriteTexture(surfaceContext.MainImageId, ImageLayout.ColorAttachment);

        foreach (var command in _commands)
        {
            config.ReadTexture(command.FillCommand.StencilImageId, ImageLayout.StencilAttachment);
            command.EdgeBufferId =
                config.CreateBuffer<EdgeInstance>(Math.Max(1, command.Edges.Count), GraphBufferUsage.HostThenGraphics);
        }
    }

    public void Execute(IPassConfig passConfig, SurfaceContext surfaceContext, ICompiledGraph graph,
        IExecutionContext ctx)
    {
        foreach (var command in _commands)
        {
            var mainImage = graph.GetImageOrException(surfaceContext.MainImageId);
            var stencilImage = graph.GetImageOrException(command.FillCommand.StencilImageId);

            ctx.BeginRendering(surfaceContext.Extent, [mainImage], stencilAttachment: stencilImage)
                .DisableFaceCulling();

            ctx.StencilCoverOp();
            if (_coverShader.Bind(ctx) is { } coverBind)
                coverBind.Push(new CoverPush
                    {
                        Projection = surfaceContext.ProjectionMatrix,
                        MinPos = command.BoundsMin,
                        MaxPos = command.BoundsMax,
                        Color = command.Color
                    })
                    .Draw(6);

            if (command.Edges.Count > 0)
            {
                var edgeBuffer = graph.GetBufferOrException(command.EdgeBufferId);
                edgeBuffer.Write(CollectionsMarshal.AsSpan(command.Edges));

                ctx.StencilPassThrough();
                if (_boundaryShader.Bind(ctx) is { } boundaryBind)
                    boundaryBind.Push(new BoundaryPush
                        {
                            Projection = surfaceContext.ProjectionMatrix,
                            EdgesAddress = edgeBuffer.GetAddress()
                        })
                        .Draw(6, (uint)command.Edges.Count);
            }

            ctx.EndRendering();
        }
    }
}
