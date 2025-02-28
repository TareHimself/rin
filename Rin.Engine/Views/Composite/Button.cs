using Rin.Engine.Views.Events;
using Rin.Engine.Core.Extensions;

namespace Rin.Engine.Views.Composite;

public class Button : Rect
{
    public event Action<CursorDownEvent, Button>? OnPressed;
    public event Action<CursorUpEvent, Button>? OnReleased;


    public override bool OnCursorDown(CursorDownEvent e)
    {
        OnPressed?.Invoke(e, this);
        return (OnReleased?.GetInvocationList().NotEmpty() ?? false) ||
               (OnPressed?.GetInvocationList().NotEmpty() ?? false);
    }

    public override void OnCursorUp(CursorUpEvent e)
    {
        base.OnCursorUp(e);
        OnReleased?.Invoke(e, this);
    }
}