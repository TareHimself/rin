namespace Rin.Engine.Views.Graphics.Commands;

public class NoOpCommand : ICommand
{
    public Type PassType => typeof(NoOpCommand);

    public IViewsPass CreatePass(PassCreateInfo createInfo)
    {
        throw new NotImplementedException();
    }

    public uint StencilMask { get; set; }
}