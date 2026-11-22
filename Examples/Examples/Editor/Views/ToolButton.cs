using System.Numerics;
using Examples.Common.Views;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;
using Rin.Core.Views.Layouts;

namespace Examples.Editor.Views;

public enum ToolButtonStyle
{
    /// <summary>
    ///     A raised button with a border.
    /// </summary>
    Normal,

    /// <summary>
    ///     The blue button for the one main action, like Play.
    /// </summary>
    Primary,

    /// <summary>
    ///     A border around the panel's own color, for an action that adds something, like Add component.
    /// </summary>
    Outline,

    /// <summary>
    ///     No fill or border until hovered, for small controls inside rows and cards.
    /// </summary>
    Quiet
}

/// <summary>
///     A rounded button with an icon and optional text, which can be shown as active or switched off.
/// </summary>
public sealed class ToolButton : CardView
{
    private const float IconSize = 12f;

    private readonly FontIconView? _icon;
    private readonly TextBoxView? _label;
    private readonly ToolButtonStyle _style;
    private readonly Action _onClicked;
    private bool _isActive;
    private bool _isEnabled = true;
    private bool _isDown;

    /// <param name="detail">Quieter text pushed to the right end, for a full-width row.</param>
    public ToolButton(char? icon, string? text, Action onClicked, ToolButtonStyle style = ToolButtonStyle.Normal,
        string? detail = null)
    {
        _style = style;
        _onClicked = onClicked;
        if (icon is { } glyph) _icon = new FontIconView { FontFamily = IconFont.Family, Icon = glyph, IconSize = IconSize };
        BorderRadius = BorderRadius.All(EditorTheme.ControlRadius);
        Padding = text is null ? new Padding(11f, 9f) : new Padding(14f, 8f);

        if (text is null)
        {
            SetChild(_icon ?? throw new ArgumentException("A button needs an icon, text or both."));
        }
        else
        {
            _label = Ui.Label(text, EditorTheme.FontSize);
            var row = Ui.Row();
            if (_icon is not null)
            {
                row.Add(Ui.Slot(_icon, fit: CrossFit.Desired, align: CrossAlign.Center));
                row.Add(Ui.Slot(Ui.Gap(0f, 8f), fit: CrossFit.Desired));
            }

            row.Add(Ui.Slot(_label, fit: CrossFit.Desired, align: CrossAlign.Center));
            if (detail is not null)
            {
                row.Add(Ui.Slot(Ui.Gap(0f, 12f), 1f));
                row.Add(Ui.Slot(Ui.Label(detail, 11f, EditorTheme.AccentSoft), fit: CrossFit.Desired, align: CrossAlign.Center));
            }

            SetChild(row);
        }

        Repaint();
    }

    /// <summary>
    ///     Stretches across the width it is given, like a row in a list.
    /// </summary>
    public bool IsFullWidth { get; init; }

    /// <summary>
    ///     Centers the content of a full-width button, like the inspector's Add component.
    /// </summary>
    public bool IsCentered { get; init; }

    public void SetIcon(char icon) => _icon?.Icon = icon;

    public void SetText(string text) => _label?.Content = text;

    public void SetActive(bool isActive)
    {
        _isActive = isActive;
        Repaint();
    }

    public void SetEnabled(bool isEnabled)
    {
        _isEnabled = isEnabled;
        Repaint();
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        if (e.Button is not CursorButton.One || !_isEnabled) return;

        _isDown = true;
        e.Target = this;
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        base.OnCursorUp(e);
        if (!_isDown) return;

        _isDown = false;
        if (IsHovered) _onClicked();
    }

    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        var size = base.ArrangeContent(availableSpace);
        if (!IsFullWidth || !float.IsFinite(availableSpace.X) || GetSlot() is not { } slot) return size;

        if (IsCentered) slot.Child.Offset = new Vector2((availableSpace.X - size.X) / 2f, 0f);
        return size with { X = availableSpace.X };
    }

    protected override void OnCursorEnter(CursorMoveSurfaceEvent e)
    {
        base.OnCursorEnter(e);
        Repaint();
    }

    protected override void OnCursorLeave()
    {
        base.OnCursorLeave();
        Repaint();
    }

    private void Repaint()
    {
        var isPrimary = _style == ToolButtonStyle.Primary || _isActive;
        var isHovered = _isEnabled && IsHovered;
        // A card draws its border as a larger rect under its fill, so a bordered button needs an opaque fill.
        Color = isPrimary ? EditorTheme.AccentFill
            : isHovered ? EditorTheme.Hover
            : _style switch
            {
                ToolButtonStyle.Normal => EditorTheme.Card,
                ToolButtonStyle.Outline => EditorTheme.Panel,
                _ => Ui.Clear
            };
        BorderColor = isPrimary ? EditorTheme.AccentBorder
            : _style == ToolButtonStyle.Normal ? EditorTheme.Border
            : _style == ToolButtonStyle.Outline ? EditorTheme.BorderStrong
            : null;

        var content = !_isEnabled ? EditorTheme.Faint
            : _style == ToolButtonStyle.Quiet ? EditorTheme.Muted
            : EditorTheme.Text;
        _icon?.Color = content;
        _label?.ForegroundColor = _style == ToolButtonStyle.Quiet && _isEnabled ? EditorTheme.Text : content;
    }
}
