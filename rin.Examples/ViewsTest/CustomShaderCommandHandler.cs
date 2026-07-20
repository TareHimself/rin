using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.CommandHandlers;
using Rin.Core.Views.Graphics.Commands;

namespace rin.Examples.ViewsTest;

public class CustomShaderCommandHandler : ICommandHandler
{
    private readonly IGraphicsShader
        _prettyShader =
            IGraphicsModule.Get()
                .MakeGraphics(
                    $"fs/{Path.Join(Global.Directory, "assets", "test", "pretty.slang").Replace('\\', '/')}");

    private CustomShaderCommand[] _commands = [];
    private uint BufferId { get; set; }

    public void Init(ICommand[] commands)
    {
        _commands = commands.Cast<CustomShaderCommand>().ToArray();
    }

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
        BufferId = config.CreateBuffer<Data>(_commands.Length, GraphBufferUsage.HostThenGraphics);
    }

    public void Execute(IPassConfig passConfig,
        SurfaceContext surfaceContext, ICompiledGraph graph, IExecutionContext ctx)
    {
        if (_prettyShader.Bind(ctx) is { } bindContext)
        {
            var view = graph.GetBufferOrException(BufferId);
            for (var i = 0; i < _commands.Length; i++)
            {
                var offset = Utils.ByteSizeOf<Data>(i);
                var myView = view.GetView<Data>(offset);
                var command = _commands[i];
                ctx.SetStencilCompareMask(command.StencilMask);
                var extent = surfaceContext.Extent;
                var screenSize = new Vector2(extent.Width, extent.Height);
                var data = new Data
                {
                    Projection = surfaceContext.ProjectionMatrix,
                    ScreenSize = screenSize,
                    Transform = command.Transform,
                    Size = command.Size,
                    Time = IApplication.Get().TimeSeconds,
                    Center = command.Hovered ? command.CursorPosition : screenSize / 2.0f
                };
                
                myView.Write(data);
                bindContext
                    .Push(myView.GetAddress())
                    .Draw(6);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [NoReorder]
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