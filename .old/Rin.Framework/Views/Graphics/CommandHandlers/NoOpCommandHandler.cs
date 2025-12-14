using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Graph;
using Rin.Framework.Views.Graphics.Commands;

namespace Rin.Framework.Views.Graphics.CommandHandlers;

public class NoOpCommandHandler : ICommandHandler
{
    private static NoOpCommandHandler? Instance { get; set; }

    public void Init(ICommand[] commands)
    {
    }

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
    }

    public void Execute(IPassConfig passConfig,
        SurfaceContext surfaceContext, ICompiledGraph graph, IExecutionContext ctx)
    {
    }
}