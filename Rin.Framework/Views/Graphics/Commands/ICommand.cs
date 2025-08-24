using Rin.Framework.Views.Graphics.CommandHandlers;

namespace Rin.Framework.Views.Graphics.Commands;

/// <summary>
///     Base class for views commands
/// </summary>
public interface ICommand
{
    public uint StencilMask { get; set; }
    public Type PassConfigType { get; }
    public Type HandlerType { get; }
    public IPassConfig CreateConfig(SurfaceContext context);
    public ICommandHandler CreateHandler(ICommand[] commands);
}