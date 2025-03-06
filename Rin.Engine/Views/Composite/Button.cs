using Rin.Engine.Views.Events;
using Rin.Engine.Core.Extensions;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Composite;

public class Button : Rect
{
    public event Action<SurfaceCursorDownEvent, Button>? OnPressed;
    public event Action<SurfaceCursorUpEvent, Button>? OnReleased;


    public override bool OnCursorDown(SurfaceCursorDownEvent e)
    {
        OnPressed?.Invoke(e, this);
        return (OnReleased?.GetInvocationList().NotEmpty() ?? false) ||
               (OnPressed?.GetInvocationList().NotEmpty() ?? false);
    }

    public override void OnCursorUp(SurfaceCursorUpEvent e)
    {
        base.OnCursorUp(e);
        OnReleased?.Invoke(e, this);
    }
}