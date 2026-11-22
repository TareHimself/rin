using System.Numerics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;

namespace Examples.Editor.Views;

/// <summary>
///     A label that changes a number while dragged left or right, in steps that scale with the value.
/// </summary>
public sealed class ScrubLabelView : TextBoxView
{
    private const float FloatStepFraction = 0.005f;
    private const float MinFloatStep = 0.01f;
    private const float IntegerStep = 0.2f;

    private readonly Func<double> _getValue;
    private readonly Action<double> _setValue;
    private readonly bool _isInteger;
    private float _startX;
    private double _startValue;
    private double _step;
    private bool _dragging;

    public ScrubLabelView(string text, Func<double> getValue, Action<double> setValue, bool isInteger)
    {
        _getValue = getValue;
        _setValue = setValue;
        _isInteger = isInteger;
        Content = text;
        FontSize = 14f;
        ForegroundColor = EditorTheme.Muted;
    }

    /// <summary>
    ///     The color while not dragged.
    /// </summary>
    public Color RestColor
    {
        get;
        init
        {
            field = value;
            ForegroundColor = value;
        }
    } = EditorTheme.Muted;

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        if (e.Button is not CursorButton.One) return;

        // A focused number input stops following its value, so it must let go before the drag changes it.
        e.Surface.ClearFocus();
        _dragging = true;
        _startX = e.Position.X;
        _startValue = _getValue();
        _step = _isInteger ? IntegerStep : double.Max(MinFloatStep, Math.Abs(_startValue) * FloatStepFraction);
        ForegroundColor = EditorTheme.Accent;
        e.Target = this;
    }

    public override void OnCursorMove(CursorMoveSurfaceEvent e, in Matrix4x4 transform)
    {
        if (_dragging) _setValue(_startValue + (e.Position.X - _startX) * _step);
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        _dragging = false;
        ForegroundColor = RestColor;
        base.OnCursorUp(e);
    }
}
