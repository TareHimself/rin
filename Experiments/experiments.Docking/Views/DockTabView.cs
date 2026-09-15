using System.Numerics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;
using Rin.Core.Views.Layouts;
using experiments.Docking.Model;

namespace experiments.Docking.Views;

/// <summary>
///     A single clickable / draggable tab in a <see cref="DockTabGroupView" />'s strip.
///     Click selects the panel; dragging past a threshold hands off to
///     <see cref="DockSpaceView" /> for redocking.
/// </summary>
public sealed class DockTabView : RectView
{
    public const float TabHeight = 30f;
    private const float DragThreshold = 5f;

    private static readonly Color IdleColor = new(0.16f, 0.16f, 0.18f, 1f);
    private static readonly Color HoverColor = new(0.22f, 0.22f, 0.25f, 1f);
    private static readonly Color ActiveColor = new(0.11f, 0.34f, 0.55f, 1f);

    private readonly DockTabGroupView _group;
    private readonly DockPanel _panel;
    private readonly int _index;

    private bool _pointerDown;
    private bool _dragging;
    private Vector2 _downPos;

    public DockTabView(DockTabGroupView group, DockPanel panel, int index)
    {
        _group = group;
        _panel = panel;
        _index = index;
        BorderRadius = new Vector4(4f, 4f, 0f, 0f);
        Padding = new Padding(10f, 6f);

        SetChild(new ListView(Axis.Row)
        {
            InitSlots =
            [
                new ListSlot
                {
                    Child = new TextBoxView { Content = panel.Title, FontSize = 13f },
                    Align = CrossAlign.Center
                },
                new ListSlot
                {
                    Child = new DockTabCloseView { OnClose = () => _group.Space.ClosePanel(_panel), Padding = new Padding(6f, 0f) },
                    Align = CrossAlign.Center
                }
            ]
        });
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        return base.ComputeDesiredContentSize() with { Y = TabHeight - Padding.Vertical };
    }

    protected override void CollectSelf(Matrix4x4 transform, CommandList cmds)
    {
        var isActive = ReferenceEquals(_group.Node.ActivePanel, _panel);
        var color = isActive ? ActiveColor : IsHovered ? HoverColor : IdleColor;
        cmds.AddRect(transform, GetSize(), color, BorderRadius);
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        if (e.Button != CursorButton.One) return;
        _group.SetActive(_index);
        _pointerDown = true;
        _dragging = false;
        _downPos = e.Position;
        e.Target = this;
    }

    public override void OnCursorMove(CursorMoveSurfaceEvent e, in Matrix4x4 transform)
    {
        if (!_pointerDown) return;

        if (!_dragging && (e.Position - _downPos).Length() >= DragThreshold)
        {
            _dragging = true;
            _group.Space.BeginDrag(_panel, e.Position);
        }

        if (_dragging) _group.Space.UpdateDrag(e.Position);
        e.Target = this;
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        if (_dragging) _group.Space.EndDrag(e.Position);
        _pointerDown = false;
        _dragging = false;
        base.OnCursorUp(e);
    }
}

/// <summary>The little "×" hit target on a tab.</summary>
internal sealed class DockTabCloseView : ContentView
{
    private const float Size = 12f;

    public Action? OnClose { get; init; }

    public override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2(Size);
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return new Vector2(Size);
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        var c = Color.White with { A = IsHovered ? 1f : 0.55f };
        commands.AddLine(transform, new Vector2(0f), new Vector2(Size), 1.5f, c);
        commands.AddLine(transform, new Vector2(Size, 0f), new Vector2(0f, Size), 1.5f, c);
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        if (e.Button != CursorButton.One) return;
        OnClose?.Invoke();
        e.Target = this;
    }
}
