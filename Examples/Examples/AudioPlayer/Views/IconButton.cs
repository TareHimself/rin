using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;

namespace Examples.AudioPlayer.Views;

/// <summary>
///     A round button of a fixed size with an icon in the middle.
/// </summary>
public sealed class IconButton : RectView
{
    private readonly FontIconView _icon;
    private readonly float _size;
    private Color _normal;
    private Color _hover;
    private Color _pressed;
    private bool _down;

    public IconButton(char icon, float size, float iconSize, Color normal, Color hover, Color pressed, Color iconColor)
    {
        _size = size;
        _normal = normal;
        _hover = hover;
        _pressed = pressed;
        _icon = new FontIconView
        {
            FontFamily = Icons.Family,
            Icon = icon,
            IconSize = iconSize,
            Color = iconColor
        };
        Color = normal;
        BorderRadius = BorderRadius.All(size / 2f);
        SetChild(_icon);
    }

    public event Action? Clicked;

    public char Icon
    {
        get => _icon.Icon;
        set => _icon.Icon = value;
    }

    public void Restyle(Color normal, Color hover, Color pressed, Color iconColor)
    {
        _normal = normal;
        _hover = hover;
        _pressed = pressed;
        _icon.Color = iconColor;
        Color = _down ? pressed : IsHovered ? hover : normal;
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2(_size);
    }

    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        var iconSize = _icon.Layout(new Vector2(float.PositiveInfinity));
        _icon.Offset = (new Vector2(_size) - iconSize) / 2f;
        return new Vector2(_size);
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        _down = true;
        Color = _pressed;
        e.Target = this;
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        if (!_down) return;

        _down = false;
        Color = IsHovered ? _hover : _normal;
        if (IsHovered) Clicked?.Invoke();
    }

    protected override void OnCursorEnter(CursorMoveSurfaceEvent e)
    {
        base.OnCursorEnter(e);
        if (!_down) Color = _hover;
    }

    protected override void OnCursorLeave()
    {
        base.OnCursorLeave();
        if (!_down) Color = _normal;
    }
}
