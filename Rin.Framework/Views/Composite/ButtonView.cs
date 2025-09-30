using Rin.Framework.Extensions;
using Rin.Framework.Views.Events;

namespace Rin.Framework.Views.Composite;

public class ButtonView : RectView
{
    public event Action<CursorDownSurfaceEvent, ButtonView>? OnPressed;
    public event Action<CursorUpSurfaceEvent, ButtonView>? OnReleased;


    public override bool OnCursorDown(CursorDownSurfaceEvent e)
    {
        OnPressed?.Invoke(e, this);
        return (OnReleased?.GetInvocationList().NotEmpty() ?? false) ||
               (OnPressed?.GetInvocationList().NotEmpty() ?? false);
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        base.OnCursorUp(e);
        OnReleased?.Invoke(e, this);
    }
}