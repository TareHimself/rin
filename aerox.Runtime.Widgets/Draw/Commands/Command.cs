

namespace aerox.Runtime.Widgets.Draw.Commands;



public abstract class Command : Disposable
{
    public abstract void Run(WidgetFrame frame);


    protected override void OnDispose(bool isManual)
    {
    }
}