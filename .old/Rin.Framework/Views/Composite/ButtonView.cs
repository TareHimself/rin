using System.Numerics;
using Rin.Framework.Extensions;
using Rin.Framework.Views.Events;

namespace Rin.Framework.Views.Composite;

public class ButtonView : RectView
{
    public event Action<CursorDownSurfaceEvent, ButtonView>? OnPressed;
    public event Action<CursorUpSurfaceEvent, ButtonView>? OnReleased;


    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        OnPressed?.Invoke(e, this);
        if ((OnReleased?.GetInvocationList().NotEmpty() ?? false) ||
            (OnPressed?.GetInvocationList().NotEmpty() ?? false))
        {
            e.Target = this;
        }
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        base.OnCursorUp(e);
        OnReleased?.Invoke(e, this);
    }
}