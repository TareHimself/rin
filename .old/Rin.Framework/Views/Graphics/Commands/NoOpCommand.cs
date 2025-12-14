using Rin.Framework.Views.Graphics.CommandHandlers;

namespace Rin.Framework.Views.Graphics.Commands;

public class NoOpCommand : ICommand
{
    public uint StencilMask { get; set; }

    public Type PassConfigType { get; } = typeof(NoOpCommand);

    public Type HandlerType { get; } = typeof(NoOpCommand);

    public IPassConfig CreateConfig(SurfaceContext context)
    {
        throw new NotImplementedException();
    }

    public ICommandHandler CreateHandler(ICommand[] commands)
    {
        throw new NotImplementedException();
    }
}