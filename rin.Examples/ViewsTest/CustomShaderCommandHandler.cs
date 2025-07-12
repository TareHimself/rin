using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Engine;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.CommandHandlers;
using Rin.Engine.Views.Graphics.Commands;

namespace rin.Examples.ViewsTest;

public class CustomShaderCommandHandler : ICommandHandler
{
    private readonly IGraphicsShader
        _prettyShader =
            SGraphicsModule.Get()
                .MakeGraphics($"fs/{Path.Join(SEngine.Directory,"assets", "test", "pretty.slang").Replace('\\', '/')}");
    
    private CustomShaderCommand[] _commands = [];
    private uint BufferId { get; set; }
    public void Init(ICommand[] commands)
    {
        _commands = commands.Cast<CustomShaderCommand>().ToArray();
    }

    public void Configure(IGraphConfig config, IPassConfig passConfig)
    {
        BufferId = config.CreateBuffer<Data>(_commands.Length, GraphBufferUsage.HostThenGraphics);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx, IPassConfig passConfig)
    {
        if (_prettyShader.Bind(ctx))
        {
            var view = graph.GetBufferOrException(BufferId);
            foreach (var customShaderCommand in _commands)
            {
                ctx.SetStencilCompareMask(customShaderCommand.StencilMask);
                var pushResource = _prettyShader.PushConstants.First().Value;
                var extent = passConfig.PassContext.Extent;
                var screenSize = new Vector2(extent.Width, extent.Height);
                var data = new Data
                {
                    Projection = passConfig.PassContext.ProjectionMatrix,
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
        }
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