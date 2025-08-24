using Rin.Framework.Graphics;
using Rin.Framework.Graphics.FrameGraph;
using Rin.Framework.Views.Graphics.CommandHandlers;

namespace Rin.Framework.Views.Graphics.Passes;

public class ViewsDrawPass : IPass, IPassWithPreAdd, IPassWithPostAdd
{
    private readonly SurfaceContext _surfaceContext;
    private readonly ICommandHandler[] _commandHandlers;
    private readonly IPassConfig _passConfig;
    private readonly List<ICommandHandlerWithPostAdd> _postHandlers = [];
    private readonly List<ICommandHandlerWithPreAdd> _preHandlers = [];

    public ViewsDrawPass(SurfaceContext surfaceContext,IPassConfig passConfig, ICommandHandler[] commandHandlers)
    {
        _surfaceContext = surfaceContext;
        _passConfig = passConfig;
        _commandHandlers = commandHandlers;
        foreach (var handler in commandHandlers)
        {
            {
                if (handler is ICommandHandlerWithPreAdd casted) _preHandlers.Add(casted);
            }
            {
                if (handler is ICommandHandlerWithPostAdd casted) _postHandlers.Add(casted);
            }
        }
    }

    public uint Id { get; set; }
    public bool IsTerminal => false;


    public void Configure(IGraphConfig config)
    {
        _passConfig.Configure(config);

        foreach (var commandHandler in _commandHandlers) commandHandler.Configure(_passConfig,_surfaceContext, config);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        _passConfig.Begin(graph, ctx);

        foreach (var commandHandler in _commandHandlers) commandHandler.Execute(_passConfig,_surfaceContext, graph, ctx);

        _passConfig.End(graph, ctx);
    }

    public void PostAdd(IGraphBuilder builder)
    {
        foreach (var handler in _postHandlers) handler.PostAdd(builder);
    }

    public void PreAdd(IGraphBuilder builder)
    {
        foreach (var handler in _preHandlers) handler.PreAdd(builder);
    }
}