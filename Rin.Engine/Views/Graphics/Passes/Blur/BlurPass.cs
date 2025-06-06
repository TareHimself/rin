using System.Diagnostics;
using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Graphics.Textures;
using Rin.Engine.Views.Graphics.Commands;

namespace Rin.Engine.Views.Graphics.Passes.Blur;

internal struct BlurData()
{
    public required ImageHandle SourceT;

    public required Matrix4x4 Projection = Matrix4x4.Identity;

    public required Matrix4x4 Transform = Matrix4x4.Identity;

    private Vector4 _options = Vector4.Zero;

    public Vector2 Size
    {
        get => new(_options.X, _options.Y);
        set
        {
            _options.X = value.X;
            _options.Y = value.Y;
        }
    }

    public float Strength
    {
        get => _options.Z;
        set => _options.Z = value;
    }

    public float Radius
    {
        get => _options.W;
        set => _options.W = value;
    }

    public Vector4 Tint = Vector4.Zero;
}

public class BlurPass : IViewsPass
{
    private readonly BlurCommand[] _blurCommands;

    private readonly IGraphicsShader _blurShader = SGraphicsModule.Get()
        .MakeGraphics("Engine/Shaders/Views/blur.slang");

    private readonly SharedPassContext _sharedContext;
    private uint _bufferId;

    public BlurPass(PassCreateInfo info)
    {
        _sharedContext = info.Context;
        _blurCommands = info.Commands.Cast<BlurCommand>().ToArray();
    }

    private uint MainImageId => _sharedContext.MainImageId;
    private uint CopyImageId => _sharedContext.CopyImageId;
    private uint StencilImageId => _sharedContext.StencilImageId;
    public uint Id { get; set; }
    public bool IsTerminal { get; } = false;

    public void Configure(IGraphConfig config)
    {
        config.WriteImage(MainImageId, ImageLayout.ColorAttachment);
        config.ReadImage(CopyImageId, ImageLayout.ShaderReadOnly);
        config.ReadImage(StencilImageId, ImageLayout.StencilAttachment);
        _bufferId = config.CreateBuffer<BlurData>(_blurCommands.Length, GraphBufferUsage.HostThenGraphics);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        if (_blurShader.Bind(ctx))
        {
            var drawImage = graph.GetImage(MainImageId);
            var copyImage = graph.GetImage(CopyImageId);
            var stencilImage = graph.GetImage(StencilImageId);
            var buffer = graph.GetBufferOrException(_bufferId);

            Debug.Assert(copyImage.BindlessHandle != ImageHandle.InvalidImage, "copyImage bindless handle is invalid");
            ctx
                .BeginRendering(_sharedContext.Extent, [drawImage], stencilAttachment: stencilImage)
                .DisableFaceCulling()
                .StencilCompareOnly();

            foreach (var blur in _blurCommands)
            {
                ctx.SetStencilCompareMask(blur.StencilMask);
                buffer.Write(new BlurData
                {
                    SourceT = copyImage.BindlessHandle,
                    Projection = _sharedContext.ProjectionMatrix,
                    Size = blur.Size,
                    Strength = blur.Strength,
                    Radius = blur.Radius,
                    Tint = blur.Tint,
                    Transform = blur.Transform
                });
                _blurShader.Push(ctx, buffer.GetAddress());
                ctx
                    .Draw(6);
            }

            ctx.EndRendering();
        }
    }

    public static IViewsPass Create(PassCreateInfo info)
    {
        return new BlurPass(info);
    }
}