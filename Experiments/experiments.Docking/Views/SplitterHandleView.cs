using System.Numerics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;
using experiments.Docking.Model;

namespace experiments.Docking.Views;

/// <summary>
///     The thin draggable divider between two panes of a <see cref="DockSplitView" />.
///     Reports the drag delta (in surface pixels, along the split's main axis) to
///     <see cref="OnDrag" />; the split turns that into a change of pane weights.
/// </summary>
public sealed class SplitterHandleView : ContentView
{
    public const float Thickness = 8f;

    private readonly DockOrientation _orientation;
    private bool _dragging;
    private Vector2 _lastPos;

    public SplitterHandleView(DockOrientation orientation)
    {
        _orientation = orientation;
    }

    /// <summary>Pixels moved along the main axis since the last call.</summary>
    public Action<float>? OnDrag { get; set; }

    public override bool IsFocusable => false;

    public override Vector2 ComputeDesiredContentSize()
    {
        return _orientation == DockOrientation.Horizontal
            ? new Vector2(Thickness, 0f)
            : new Vector2(0f, Thickness);
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        // Fill the cross axis, stay fixed on the main axis.
        return _orientation == DockOrientation.Horizontal
            ? new Vector2(Thickness, float.IsFinite(availableSpace.Y) ? availableSpace.Y : 0f)
            : new Vector2(float.IsFinite(availableSpace.X) ? availableSpace.X : 0f, Thickness);
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        var active = _dragging || IsHovered;
        var color = Color.White with { A = active ? 0.35f : 0.12f };
        commands.AddRect(transform, GetContentSize(), color);
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        if (e.Button != CursorButton.One) return;
        _dragging = true;
        _lastPos = e.Position;
        e.Target = this;
    }

    public override void OnCursorMove(CursorMoveSurfaceEvent e, in Matrix4x4 transform)
    {
        if (!_dragging) return;
        var deltaVec = e.Position - _lastPos;
        _lastPos = e.Position;
        var delta = _orientation == DockOrientation.Horizontal ? deltaVec.X : deltaVec.Y;
        if (delta != 0f) OnDrag?.Invoke(delta);
        e.Target = this;
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        _dragging = false;
        base.OnCursorUp(e);
    }
}
