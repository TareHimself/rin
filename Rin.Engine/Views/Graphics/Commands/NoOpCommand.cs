using Rin.Engine.Views.Graphics.CommandHandlers;

namespace Rin.Engine.Views.Graphics.Commands;

public class NoOpCommand : ICommand
{
    public uint StencilMask { get; set; }
    
    public Type PassConfigType { get; } = typeof(NoOpCommand);
    
    public Type HandlerType { get; } = typeof(NoOpCommand);
    
    public IPassConfig CreateConfig(SurfacePassContext context)
    {
        throw new NotImplementedException();
    }

    public ICommandHandler CreateHandler(ICommand[] commands)
    {
        throw new NotImplementedException();
    }
}