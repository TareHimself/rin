using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Engine;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Views.Graphics;

namespace rin.Examples.ViewsTest;

public class CustomShaderPass(PassCreateInfo info) : IViewsPass
{
    private readonly IGraphicsShader
        _prettyShader =
            SGraphicsModule.Get()
                .MakeGraphics($"fs/{Path.Join(SEngine.Directory,"assets", "test", "pretty.slang").Replace('\\', '/')}");

    private CustomShaderCommand[] _customCommands = [];
    public uint MainImageId => info.Context.MainImageId;
    public uint CopyImageId => info.Context.CopyImageId;
    public uint StencilImageId => info.Context.StencilImageId;
    private uint BufferId { get; set; }
    public uint Id { get; set; }
    public bool IsTerminal => false;

    public void Configure(IGraphConfig config)
    {
        config.WriteImage(MainImageId, ImageLayout.ColorAttachment);
        config.ReadImage(StencilImageId, ImageLayout.StencilAttachment);
        _customCommands = info.Commands.Cast<CustomShaderCommand>().ToArray();
        BufferId = config.CreateBuffer<Data>(_customCommands.Length, GraphBufferUsage.HostThenGraphics);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        if (_prettyShader.Bind(ctx))
        {
            var drawImage = graph.GetImage(MainImageId);
            var stencilImage = graph.GetImage(StencilImageId);
            var view = graph.GetBufferOrException(BufferId);

            ctx.BeginRendering(info.Context.Extent, [drawImage], stencilAttachment: stencilImage)
                .DisableFaceCulling()
                .StencilCompareOnly();
            foreach (var customShaderCommand in _customCommands)
            {
                ctx.SetStencilCompareMask(customShaderCommand.StencilMask);
                var pushResource = _prettyShader.PushConstants.First().Value;
                var extent = info.Context.Extent;
                var screenSize = new Vector2(extent.Width, extent.Height);
                var data = new Data
                {
                    Projection = info.Context.ProjectionMatrix,
                    ScreenSize = screenSize,
                    Transform = customShaderCommand.Transform,
                    Size = customShaderCommand.Size,
                    Time = SEngine.Get().GetTimeSeconds(),
                    Center = customShaderCommand.Hovered ? customShaderCommand.CursorPosition : screenSize / 2.0f
                };
                view.WriteStruct(data);
                _prettyShader.Push(ctx, view.GetAddress());
                ctx
                    .Draw(6);
            }

            ctx.EndRendering();
        }
    }

    public static IViewsPass Create(PassCreateInfo info)
    {
        return new CustomShaderPass(info);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Data
    {
        public required Matrix4x4 Projection;
        public required Vector2 ScreenSize;
        public required Matrix4x4 Transform;
        public required Vector2 Size;
        public required float Time;
        public required Vector2 Center;
    }
}