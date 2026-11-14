using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Shared;
using Rin.Core.Views.Graphics.CommandHandlers;

namespace Rin.Core.Views.Graphics.Passes;

/// <summary>
///     Runs its handlers in one pass. It keeps them in a pooled buffer, released on dispose.
/// </summary>
public class ViewsDrawPass : IPassWithPreAdd, IDisposable
{
    private readonly IPassConfig _passConfig;
    private readonly SurfaceContext _surfaceContext;
    private PooledMemory<ICommandHandler> _commandHandlers;
    private bool _disposed;

    public ViewsDrawPass(SurfaceContext surfaceContext, IPassConfig passConfig,
        ReadOnlySpan<ICommandHandler> commandHandlers)
    {
        _surfaceContext = surfaceContext;
        _passConfig = passConfig;
        _commandHandlers = PooledMemory<ICommandHandler>.CopyFrom(commandHandlers);
    }

    private ReadOnlySpan<ICommandHandler> Handlers => _disposed ? default : _commandHandlers.AsSpan();

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        _passConfig.Configure(config);

        foreach (var commandHandler in Handlers) commandHandler.Configure(_passConfig, _surfaceContext, config);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        _passConfig.Begin(graph, ctx);

        foreach (var commandHandler in Handlers)
            commandHandler.Execute(_passConfig, _surfaceContext, graph, ctx);

        _passConfig.End(graph, ctx);
    }

    public void PreAdd(IGraphBuilder builder)
    {
        foreach (var handler in Handlers)
            if (handler is ICommandHandlerWithPreAdd preHandler)
                preHandler.PreAdd(builder);
    }

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var handler in Handlers) (handler as IDisposable)?.Dispose();
        _disposed = true;
        _commandHandlers.Dispose();
    }
}