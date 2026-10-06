using System.Numerics;
using Rin.Core.Animation;
using Rin.Core.Views;
using Rin.Core.Views.Animation;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;

namespace Examples;

public sealed class LauncherButton : ButtonView
{
    private const float ContentWidth = 260f;
    // Text measures shorter without descenders, so the height is fixed to keep every button the same.
    private const float ContentHeight = 24f;
    private const float PressedScale = 0.96f;
    private const float PressDuration = 0.06f;
    private const float ReleaseDuration = 0.1f;
    private const float ColorBlendRate = 14f;

    private static readonly Color Normal = new(0.2f, 0.24f, 0.33f, 1f);
    private static readonly Color Hover = new(0.28f, 0.34f, 0.48f, 1f);
    private static readonly Color Pressed = new(0.15f, 0.18f, 0.26f, 1f);

    private readonly Action _clicked;
    private readonly TextBoxView _label;
    private bool _down;

    public LauncherButton(string text, Action clicked)
    {
        _clicked = clicked;
        Color = Normal;
        BorderRadius = new Vector4(10f);
        Padding = new Padding(20f, 12f);
        Pivot = new Vector2(0.5f);
        _label = new TextBoxView { Content = text, FontSize = 18f };
        SetChild(_label);
    }

    public override Vector2 Layout(in Vector2 availableSpace, bool fill = false)
    {
        var size = base.Layout(availableSpace, fill);
        // A pivot moves the view by its size times the pivot, so this puts it back where the layout placed it.
        Translate = size * Pivot;
        return size;
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2(ContentWidth, ContentHeight);
    }

    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        var labelSize = _label.Layout(new Vector2(ContentWidth, ContentHeight));
        _label.Offset = new Vector2((ContentWidth - labelSize.X) / 2f, 0f);
        return new Vector2(ContentWidth, ContentHeight);
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        _down = true;
        e.Target = this;
        this.StopAll().ScaleTo(new Vector2(PressedScale), PressDuration);
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        if (!_down) return;

        _down = false;
        var release = this.StopAll().ScaleTo(Vector2.One, ReleaseDuration);
        if (IsHovered) release.After().Do(_clicked);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        var target = _down ? Pressed : IsHovered ? Hover : Normal;
        var blend = float.Min(deltaTime * ColorBlendRate, 1f);
        Color = Color * (1f - blend) + target * blend;
    }
}
