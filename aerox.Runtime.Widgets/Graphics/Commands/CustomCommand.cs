namespace aerox.Runtime.Widgets.Graphics.Commands;

public abstract class CustomCommand : Command
{
    public abstract void Run(WidgetFrame frame,uint stencilMask);
}