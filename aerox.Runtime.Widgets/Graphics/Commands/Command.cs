

namespace aerox.Runtime.Widgets.Graphics.Commands;



public abstract class Command : Disposable
{
    
    public virtual bool CanCombineWith(Command other) => false;

    protected override void OnDispose(bool isManual)
    {
    }
}