using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Views.Graphics.CommandHandlers;

namespace Rin.Engine.Views.Graphics.Passes;

public class ViewsDrawPass : IPass, IPassWithPreAdd, IPassWithPostAdd
{
    public uint Id { get; set; }
    public bool IsTerminal => false;

    private readonly List<ICommandHandlerWithPreAdd> _preHandlers = [];
    private readonly List<ICommandHandlerWithPostAdd> _postHandlers = [];
    private readonly IPassConfig _passConfig;
    private readonly ICommandHandler[] _commandHandlers;

    public ViewsDrawPass(IPassConfig passConfig,ICommandHandler[] commandHandlers)
    {
        _passConfig = passConfig;
        _commandHandlers = commandHandlers;
        foreach (var handler in commandHandlers)
        {
            {
                if (handler is ICommandHandlerWithPreAdd casted)
                {
                    _preHandlers.Add(casted);
                }
            }
            {
                if (handler is ICommandHandlerWithPostAdd casted)
                {
                    _postHandlers.Add(casted);
                }
            }
        }
    }


    public void Configure(IGraphConfig config)
    {
        _passConfig.Configure(config);
        
        foreach (var commandHandler in _commandHandlers)
        {
            commandHandler.Configure(config,_passConfig);
        }
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        _passConfig.Begin(graph, ctx);
        
        foreach (var commandHandler in _commandHandlers)
        {
            commandHandler.Execute(graph, ctx, _passConfig);
        }
        
        _passConfig.End(graph, ctx);
    }

    public void PostAdd(IGraphBuilder builder)
    {
        foreach (var handler in _postHandlers)
        {
            handler.PostAdd(builder);
        }
    }

    public void PreAdd(IGraphBuilder builder)
    {
        foreach (var handler in _preHandlers)
        {
            handler.PreAdd(builder);
        }
    }
}