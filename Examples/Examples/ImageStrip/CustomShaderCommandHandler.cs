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
using Rin.Shade;
using Examples.ImageStrip.Shaders;

namespace Examples.ImageStrip;

public partial class CustomShaderCommandHandler : ICommandHandler
{
    [GraphicsShader<PrettyShader>]
    private partial IGraphicsShader Shader { get; }

    private CustomShaderCommand[] _commands = [];
    private uint BufferId { get; set; }

    public void Init(ICommand[] commands)
    {
        _commands = commands.Cast<CustomShaderCommand>().ToArray();
    }

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
        BufferId = config.CreateBuffer<PrettyData>(_commands.Length, GraphBufferUsage.HostThenGraphics);
    }

    public void Execute(IPassConfig passConfig,
        SurfaceContext surfaceContext, ICompiledGraph graph, IExecutionContext ctx)
    {
        if (Shader.Bind(ctx) is { } bindContext)
        {
            var view = graph.GetBufferOrException(BufferId);
            for (var i = 0; i < _commands.Length; i++)
            {
                var offset = Utils.ByteSizeOf<PrettyData>(i);
                var myView = view.GetView<PrettyData>(offset);
                var command = _commands[i];
                ctx.SetStencilCompareMask(command.StencilMask);
                var data = new PrettyData
                {
                    Projection = surfaceContext.ProjectionMatrix,
                    Transform = command.Transform,
                    BorderRadius = command.BorderRadius,
                    Size = command.Size,
                    Focus = command.Focus,
                    Time = IApplication.Get().TimeSeconds
                };

                myView.WriteSingle(data);
                bindContext
                    .Push(new PrettyShader.PushConstants { Data = new BufferRef<PrettyData>(myView.GetAddress()) })
                    .Draw(6);
            }
        }
    }
}
