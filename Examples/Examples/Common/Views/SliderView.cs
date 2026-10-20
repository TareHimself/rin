using System.Numerics;
using Examples.Common.Views;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;
using Rin.Core.Views.Layouts;

namespace Examples.Common.Views;

/// <summary>
///     A slider over a 0 to 1 value, left to right as a <see cref="Axis.Row" /> or bottom to top as a
///     <see cref="Axis.Column" />. While dragging it shows the dragged value and commits on release, or on every
///     move with <see cref="LiveUpdate" />.
/// </summary>
public sealed class SliderView(Func<float> getValue, Action<float> onChanged) : ContentView
{
    private const float DefaultLength = 200f;
    private float? _dragValue;

    public Axis Axis { get; init; } = Axis.Row;

    public float TrackHeight { get; init; } = 6f;

    public float KnobRadius { get; init; } = 7f;

    public Color TrackColor { get; init; } = Ui.Raised;

    public Color FillColor { get; init; } = Ui.Accent;

    public Color KnobColor { get; init; } = Color.White;

    /// <summary>
    ///     The value the filled part of the track starts from, 0.5 for a slider centred on zero.
    /// </summary>
    public float FillOrigin { get; init; }

    public bool KnobAlwaysVisible { get; init; }

    public bool LiveUpdate { get; init; }

    public bool IsDragging => _dragValue is not null;

    public override Vector2 ComputeDesiredContentSize()
    {
        return SizeFor(DefaultLength);
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        var available = Axis == Axis.Row ? availableSpace.X : availableSpace.Y;
        return SizeFor(float.IsFinite(available) ? available : DefaultLength);
    }

    private Vector2 SizeFor(float length)
    {
        var thickness = KnobRadius * 2f;
        return Axis == Axis.Row ? new Vector2(length, thickness) : new Vector2(thickness, length);
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        var size = GetContentSize();
        var value = _dragValue ?? float.Clamp(getValue(), 0f, 1f);
        var radius = BorderRadius.All(TrackHeight / 2f);
        var length = Axis == Axis.Row ? size.X : size.Y;
        var fillStart = float.Min(FillOrigin, value) * length;
        var fillLength = float.Max(MathF.Abs(value - FillOrigin) * length, TrackHeight);

        if (Axis == Axis.Row)
        {
            var top = (size.Y - TrackHeight) / 2f;
            commands.AddRect(transform.Translate(new Vector2(0f, top)), new Vector2(size.X, TrackHeight), TrackColor,
                radius);
            commands.AddRect(transform.Translate(new Vector2(fillStart, top)), new Vector2(fillLength, TrackHeight),
                FillColor, radius);
        }
        else
        {
            var left = (size.X - TrackHeight) / 2f;
            commands.AddRect(transform.Translate(new Vector2(left, 0f)), new Vector2(TrackHeight, size.Y), TrackColor,
                radius);
            commands.AddRect(transform.Translate(new Vector2(left, size.Y - fillStart - fillLength)),
                new Vector2(TrackHeight, fillLength), FillColor, radius);
        }

        if (KnobAlwaysVisible || IsHovered || IsDragging)
            commands.AddCircle(transform, KnobCenter(size, value), KnobRadius, KnobColor);
    }

    private Vector2 KnobCenter(Vector2 size, float value)
    {
        return Axis == Axis.Row
            ? new Vector2(size.X * value, size.Y / 2f)
            : new Vector2(size.X / 2f, size.Y * (1f - value));
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        if (e.Button is not CursorButton.One) return;

        Drag(e.Position);
        e.Target = this;
    }

    public override void OnCursorMove(CursorMoveSurfaceEvent e, in Matrix4x4 transform)
    {
        if (!IsDragging) return;

        Drag(e.Position);
        e.Target = this;
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        if (IsDragging)
        {
            onChanged(ValueAt(e.Position));
            _dragValue = null;
        }

        base.OnCursorUp(e);
    }

    private void Drag(Vector2 surfacePosition)
    {
        _dragValue = ValueAt(surfacePosition);
        if (LiveUpdate) onChanged(_dragValue.Value);
    }

    private float ValueAt(Vector2 surfacePosition)
    {
        var local = surfacePosition.Transform(ComputeAbsoluteContentTransform().Inverse());
        var size = GetContentSize();
        var fraction = Axis == Axis.Row ? local.X / size.X : 1f - local.Y / size.Y;
        return float.Clamp(fraction, 0f, 1f);
    }
}
