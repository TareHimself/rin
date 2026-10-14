using System.Numerics;
using Rin.Core.Views.Events;

namespace Rin.Core.Views.Composite;

public class ButtonView : RectView
{
    public event Action<CursorDownSurfaceEvent, ButtonView>? OnPressed;
    public event Action<CursorUpSurfaceEvent, ButtonView>? OnReleased;


    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        OnPressed?.Invoke(e, this);
        e.Target = this;
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        base.OnCursorUp(e);
        OnReleased?.Invoke(e, this);
    }
}