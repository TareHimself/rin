using Rin.Engine.Extensions;
using Rin.Engine.Views.Events;

namespace Rin.Engine.Views.Composite;

public class Button : Rect
{
    public event Action<CursorDownSurfaceEvent, Button>? OnPressed;
    public event Action<CursorUpSurfaceEvent, Button>? OnReleased;


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