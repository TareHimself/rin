using System.Numerics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;

namespace Examples.Paint.Views;

/// <summary>
///     Keeps every stroke as a list of points and redraws them all each frame as line segments with round joints.
///     Dragging with the left button draws, dragging with the right button erases by removing any stroke it touches.
/// </summary>
public sealed class CanvasView : ContentView
{
    private const float MinimumPointSpacing = 1.5f;
    private readonly List<Stroke> _strokes = [];
    private readonly List<Change> _history = [];
    private Stroke? _current;
    private Vector2? _eraseFrom;

    public Color BrushColor { get; set; } = Color.Black;

    public float BrushSize { get; set; } = 8f;

    public Color Paper { get; set; } = Color.FromRgb(0.98f, 0.97f, 0.95f);

    public int StrokeCount => _strokes.Count;

    public void Undo()
    {
        if (_history.Count == 0) return;
        var change = _history[^1];
        _history.RemoveAt(_history.Count - 1);
        if (change.Added) _strokes.Remove(change.Strokes[0]);
        else _strokes.InsertRange(int.Min(change.Index, _strokes.Count), change.Strokes);
    }

    public void Clear()
    {
        if (_strokes.Count == 0) return;
        _history.Add(new Change(_strokes.ToArray(), 0, false));
        _strokes.Clear();
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return availableSpace;
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        var size = GetContentSize();
        commands.AddRect(transform, size, Paper, BorderRadius.All(12f));

        foreach (var stroke in _strokes) Draw(stroke, transform, commands);
        if (_current is not null) Draw(_current, transform, commands);
        if (IsHovered && _current is null && _eraseFrom is null) DrawBrushPreview(transform, commands);
    }

    private static void Draw(Stroke stroke, in Matrix4x4 transform, CommandList commands)
    {
        var points = stroke.Points;
        var radius = stroke.Thickness / 2f;
        commands.AddCircle(transform, points[0], radius, stroke.Color);
        for (var i = 1; i < points.Count; i++)
        {
            commands.AddLine(transform, points[i - 1], points[i], stroke.Thickness, stroke.Color);
            commands.AddCircle(transform, points[i], radius, stroke.Color);
        }
    }

    private void DrawBrushPreview(in Matrix4x4 transform, CommandList commands)
    {
        if (Surface is null) return;
        var local = ToLocal(Surface.GetCursorPosition());
        commands.AddCircle(transform, local, BrushSize / 2f + 1.5f, BrushColor with { A = 0.35f });
        commands.AddCircle(transform, local, BrushSize / 2f, Paper);
        commands.AddCircle(transform, local, BrushSize / 2f, BrushColor with { A = 0.5f });
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        var local = ToLocal(e.Position);
        switch (e.Button)
        {
            case CursorButton.One:
                _current = new Stroke(BrushColor, BrushSize);
                _current.Points.Add(local);
                break;
            case CursorButton.Two:
                _eraseFrom = local;
                EraseAlong(local, local);
                break;
            default:
                return;
        }

        e.Target = this;
    }

    public override void OnCursorMove(CursorMoveSurfaceEvent e, in Matrix4x4 transform)
    {
        var local = ToLocal(e.Position);
        if (_current is { } stroke)
        {
            if (Vector2.Distance(stroke.Points[^1], local) >= MinimumPointSpacing) stroke.Points.Add(local);
            e.Target = this;
        }
        else if (_eraseFrom is { } from)
        {
            EraseAlong(from, local);
            _eraseFrom = local;
            e.Target = this;
        }
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        if (_current is { } stroke)
        {
            _strokes.Add(stroke);
            _history.Add(new Change([stroke], _strokes.Count - 1, true));
            _current = null;
        }

        _eraseFrom = null;
        base.OnCursorUp(e);
    }

    /// <summary>
    ///     The cursor arrives in jumps, so the whole path since the last sample is tested, or a quick swipe would
    ///     pass between two samples without touching a thin stroke.
    /// </summary>
    private void EraseAlong(Vector2 from, Vector2 to)
    {
        for (var i = _strokes.Count - 1; i >= 0; i--)
        {
            if (!Touches(_strokes[i], from, to, BrushSize / 2f)) continue;
            _history.Add(new Change([_strokes[i]], i, false));
            _strokes.RemoveAt(i);
        }
    }

    private static bool Touches(Stroke stroke, Vector2 from, Vector2 to, float radius)
    {
        var reach = radius + stroke.Thickness / 2f;
        var points = stroke.Points;
        if (points.Count == 1) return DistanceToSegment(points[0], from, to) <= reach;
        for (var i = 1; i < points.Count; i++)
            if (SegmentDistance(from, to, points[i - 1], points[i]) <= reach)
                return true;
        return false;
    }

    private static float SegmentDistance(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1)
    {
        if (SegmentsCross(a0, a1, b0, b1)) return 0f;
        return float.Min(
            float.Min(DistanceToSegment(a0, b0, b1), DistanceToSegment(a1, b0, b1)),
            float.Min(DistanceToSegment(b0, a0, a1), DistanceToSegment(b1, a0, a1)));
    }

    private static bool SegmentsCross(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1)
    {
        var d1 = Cross(b1 - b0, a0 - b0);
        var d2 = Cross(b1 - b0, a1 - b0);
        var d3 = Cross(a1 - a0, b0 - a0);
        var d4 = Cross(a1 - a0, b1 - a0);
        return d1 * d2 < 0f && d3 * d4 < 0f;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - a.Y * b.X;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var lengthSquared = ab.LengthSquared();
        var t = lengthSquared <= 0f ? 0f : float.Clamp(Vector2.Dot(point - a, ab) / lengthSquared, 0f, 1f);
        return Vector2.Distance(point, a + ab * t);
    }

    private Vector2 ToLocal(Vector2 surfacePosition)
    {
        Matrix4x4.Invert(ComputeAbsoluteContentTransform(), out var toLocal);
        return Vector2.Transform(surfacePosition, toLocal);
    }

    private readonly record struct Change(Stroke[] Strokes, int Index, bool Added);
}
