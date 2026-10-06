using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;

namespace P2PChat.Views;

/// <summary>
///     A rounded button with hover and pressed colors and a centered label.
/// </summary>
public sealed class ActionButton : ButtonView
{
    private readonly TextBoxView _label;
    private Color _normal;
    private Color _hover;
    private Color _pressed;
    private bool _down;

    public ActionButton(string text, Color normal, Color hover, Color pressed, Color textColor, float fontSize = 17f)
    {
        _label = Ui.Label(text, fontSize, textColor);
        _normal = normal;
        _hover = hover;
        _pressed = pressed;
        Color = normal;
        BorderRadius = new Vector4(12f);
        Padding = new Padding(20f, 11f);
        SetChild(_label);
    }

    public event Action? Clicked;

    public void SetText(string text)
    {
        _label.Content = text;
    }

    public void Restyle(Color normal, Color hover, Color pressed, Color textColor)
    {
        _normal = normal;
        _hover = hover;
        _pressed = pressed;
        _label.ForegroundColor = textColor;
        Color = IsHovered ? hover : normal;
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

    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        var labelSize = _label.Layout(availableSpace);
        var width = float.IsFinite(availableSpace.X) ? availableSpace.X : labelSize.X;
        var size = new Vector2(width, labelSize.Y);
        _label.Offset = new Vector2((size.X - labelSize.X) / 2f, 0f);
        return size;
    }
}
