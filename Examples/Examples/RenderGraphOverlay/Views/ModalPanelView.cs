using System.Numerics;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Events;

namespace Examples.RenderGraphOverlay.Views;

public sealed class ModalPanelView : RectView
{
    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        e.Target = this;
    }

    public override void OnCursorMove(CursorMoveSurfaceEvent e, in Matrix4x4 transform)
    {
        e.Target = this;
    }

    protected override bool OnScroll(ScrollSurfaceEvent e)
    {
        return true;
    }
}
