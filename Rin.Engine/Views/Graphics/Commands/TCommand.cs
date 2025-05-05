namespace Rin.Engine.Views.Graphics.Commands;

/// <summary>
///     Base class for commands
/// </summary>
public abstract class TCommand<TPass> : ICommand where TPass : IViewsPass
{
    public Type PassType => typeof(TPass);

    public IViewsPass CreatePass(PassCreateInfo info)
    {
        return TPass.Create(info);
    }

    public uint StencilMask { get; set; }
}