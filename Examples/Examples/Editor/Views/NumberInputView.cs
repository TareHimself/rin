using System.Globalization;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;

namespace Examples.Editor.Views;

/// <summary>
///     A number that follows its source while not focused, and is written back on Enter or when focus leaves.
/// </summary>
public sealed class NumberInputView(Func<double> getValue, Action<double> setValue, bool isInteger, bool isReadOnly)
    : TextInputBoxView
{
    private double _shown = double.NaN;
    private bool _selectAllOnRelease;

    public override bool IsFocusable => !isReadOnly;

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (IsFocused) return;

        var value = getValue();
        if (value.Equals(_shown)) return;

        _shown = value;
        Content = Format(value);
    }

    // Rounded first, so a tiny negative such as -0.0001 shows as 0 rather than -0.
    private string Format(double value)
    {
        var rounded = Math.Round(value, isInteger ? 0 : 3);
        return (rounded == 0 ? 0 : rounded).ToString("0.###", CultureInfo.InvariantCulture);
    }

    public override void OnKeyboard(KeyboardSurfaceEvent e)
    {
        if (e is { Key: InputKey.Enter, State: InputState.Pressed })
            e.Surface.ClearFocus();
        else
            base.OnKeyboard(e);
    }

    public override void OnCharacter(CharacterSurfaceEvent e)
    {
        if (e.Character is not ('\n' or '\r')) base.OnCharacter(e);
    }

    // The click that focuses the field places the caret on release, so the text is selected after it.
    public override void OnFocus()
    {
        base.OnFocus();
        _selectAllOnRelease = true;
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        base.OnCursorUp(e);
        if (!_selectAllOnRelease) return;

        _selectAllOnRelease = false;
        SelectAll();
    }

    public override void OnFocusLost()
    {
        base.OnFocusLost();
        if (double.TryParse(Content, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) setValue(value);
        _shown = double.NaN;
    }
}
