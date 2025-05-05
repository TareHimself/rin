namespace Rin.Engine.Views.Graphics.Commands;

/// <summary>
///     Base class for views commands
/// </summary>
public interface ICommand
{
    public Type PassType { get; }

    public uint StencilMask { get; set; }
    public IViewsPass CreatePass(PassCreateInfo createInfo);
}