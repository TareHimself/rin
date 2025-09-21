using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Framework;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Graph;
using Rin.Framework.Graphics.Shaders;
using Rin.Framework.Graphics.Vulkan.Graph;
using Rin.Framework.Views.Graphics;
using Rin.Framework.Views.Graphics.CommandHandlers;
using Rin.Framework.Views.Graphics.Commands;

namespace rin.Examples.ViewsTest;

public class CustomShaderCommandHandler : ICommandHandler
{
    private readonly IGraphicsShader
        _prettyShader =
            IGraphicsModule.Get()
                .MakeGraphics($"fs/{Path.Join(SFramework.Directory,"assets", "test", "pretty.slang").Replace('\\', '/')}");
    
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
        if (_prettyShader.Bind(ctx) is {} bindContext)
        {
            var view = graph.GetBufferOrException(BufferId);
            foreach (var customShaderCommand in _commands)
            {
                ctx.SetStencilCompareMask(customShaderCommand.StencilMask);
                var extent = surfaceContext.Extent;
                var screenSize = new Vector2(extent.Width, extent.Height);
                var data = new Data
                {
                    Projection = surfaceContext.ProjectionMatrix,
                    ScreenSize = screenSize,
                    Transform = customShaderCommand.Transform,
                    Size = customShaderCommand.Size,
                    Time = IApplication.Get().TimeSeconds,
                    Center = customShaderCommand.Hovered ? customShaderCommand.CursorPosition : screenSize / 2.0f
                };
                view.Write(data);
                bindContext
                    .Push(view.GetAddress())
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