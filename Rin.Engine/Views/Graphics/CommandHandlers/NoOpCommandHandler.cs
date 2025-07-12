using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Views.Graphics.Commands;

namespace Rin.Engine.Views.Graphics.CommandHandlers;

public class NoOpCommandHandler : ICommandHandler
{
    public void Init(ICommand[] commands)
    {
        
    }

    public void Configure(IGraphConfig config, IPassConfig passConfig)
    {
        
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx, IPassConfig passConfig)
    {
        
    }
    
    private static NoOpCommandHandler? Instance { get; set; }
}