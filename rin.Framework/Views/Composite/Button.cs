using rin.Framework.Core.Extensions;
using rin.Framework.Core.Math;
using rin.Framework.Views.Events;

namespace rin.Framework.Views.Composite;

public class Button : Rect
{
    
    
    public event Action<CursorDownEvent,Button>? OnPressed; 
    public event Action<CursorUpEvent,Button>? OnReleased;
    

    public override bool OnCursorDown(CursorDownEvent e)
    {
        OnPressed?.Invoke(e,this);
        return (OnReleased?.GetInvocationList().NotEmpty() ?? false) || (OnPressed?.GetInvocationList().NotEmpty() ?? false);
    }

    public override void OnCursorUp(CursorUpEvent e)
    {
        base.OnCursorUp(e);
        OnReleased?.Invoke(e,this);
    }
}