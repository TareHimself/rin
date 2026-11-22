using System.Numerics;
using Examples.Common.Views;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;

namespace Examples.Editor.Views;

/// <summary>
///     A checkbox that follows a live value and calls back when clicked, or only shows the value when read-only.
/// </summary>
public sealed class CheckBoxView : ButtonView
{
    private const float BoxSize = 16f;

    private readonly Func<bool> _getValue;
    private readonly FontIconView _check;
    private readonly bool _isReadOnly;

    public CheckBoxView(Func<bool> getValue, Action? onClicked)
    {
        _getValue = getValue;
        _isReadOnly = onClicked is null;
        _check = new FontIconView { FontFamily = IconFont.Family, Icon = EditorIcons.Check, IconSize = 9f, Color = EditorTheme.Text };
        BorderRadius = BorderRadius.All(4f);
        SetChild(_check);
        if (onClicked is not null) OnReleased += (_, _) => onClicked();
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        var isOn = _getValue();
        Color = !isOn ? EditorTheme.BorderStrong
            : _isReadOnly ? EditorTheme.Faint
            : EditorTheme.Accent;
        _check.Visibility = isOn ? Visibility.Visible : Visibility.Hidden;
    }

    public override Vector2 ComputeDesiredContentSize() => new(BoxSize);

    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        var checkSize = _check.Layout(new Vector2(float.PositiveInfinity));
        _check.Offset = (new Vector2(BoxSize) - checkSize) / 2f;
        return new Vector2(BoxSize);
    }
}
