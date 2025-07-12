using Rin.Engine.Views.Graphics.CommandHandlers;

namespace Rin.Engine.Views.Graphics.Commands;

/// <summary>
/// Base class for commands
/// </summary>
public abstract class TCommand<TPassConfig,TCommandHandler> : ICommand where TPassConfig : IPassConfig , new() where TCommandHandler : ICommandHandler, new()
{
    public uint StencilMask { get; set; }
    public Type PassConfigType { get; } = typeof(TPassConfig);
    public Type HandlerType { get; } = typeof(TCommandHandler);
    public IPassConfig CreateConfig(SurfacePassContext context) => new TPassConfig
    {
        PassContext = context
    };

    public ICommandHandler CreateHandler(ICommand[] commands)
    {
        var handler = new TCommandHandler();
        handler.Init(commands);
        return handler;
    }
}