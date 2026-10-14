using System.Numerics;
using Rin.Core.Views.Font;

namespace Rin.Core.Views.Sdf;

public enum SegmentKind
{
    Line,
    Quadratic,
    Cubic
}

public readonly record struct Segment(SegmentKind Kind, Vector2 Control1, Vector2 Control2, Vector2 To);

/// <summary>
///     One closed contour of a glyph, as a start point and the segments that return to it.
/// </summary>
public sealed class Contour(Vector2 start)
{
    private const int CurveSamples = 4;

    public Vector2 Start => start;

    public List<Segment> Segments { get; } = [];

    /// <summary>
    ///     Positive when the contour winds counter-clockwise in the coordinates it is stored in.
    /// </summary>
    public float SignedArea()
    {
        var previous = start;
        var area = 0f;
        foreach (var segment in Segments)
        {
            if (segment.Kind == SegmentKind.Line)
            {
                area += Cross(previous, segment.To);
                previous = segment.To;
                continue;
            }

            for (var sample = 1; sample <= CurveSamples; sample++)
            {
                var point = PointOn(segment, previous, sample / (float)CurveSamples);
                area += Cross(previous, point);
                previous = point;
            }
        }

        area += Cross(previous, start);
        return area / 2f;
    }

    /// <summary>
    ///     The same shape traced in the opposite direction.
    /// </summary>
    public Contour Reversed()
    {
        var reversed = new Contour(Segments.Count > 0 ? Segments[^1].To : start);
        for (var i = Segments.Count - 1; i >= 0; i--)
        {
            var segment = Segments[i];
            var to = i == 0 ? start : Segments[i - 1].To;
            reversed.Segments.Add(segment.Kind == SegmentKind.Cubic
                ? segment with { Control1 = segment.Control2, Control2 = segment.Control1, To = to }
                : segment with { To = to });
        }

        return reversed;
    }

    private static Vector2 PointOn(in Segment segment, Vector2 from, float t)
    {
        var u = 1f - t;
        return segment.Kind == SegmentKind.Quadratic
            ? u * u * from + 2f * u * t * segment.Control1 + t * t * segment.To
            : u * u * u * from + 3f * u * u * t * segment.Control1 + 3f * u * t * t * segment.Control2 +
              t * t * t * segment.To;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - a.Y * b.X;
    }
}

/// <summary>
///     Collects a glyph's contours from HarfBuzz, with Y negated and scaled, and orients them so that the outer
///     contour winds counter-clockwise, which is what msdfgen expects.
/// </summary>
/// <remarks>
///     Fonts do not agree on winding: glyf outlines are usually clockwise and CFF outlines counter-clockwise, but a
///     glyf font built from SVG icons can go either way. So the direction is read off the geometry instead of the
///     outline table: the contour with the largest area is the outer one, and every contour is reversed when it
///     winds clockwise. Holes keep their relative direction, which is all the fill rule needs.
/// </remarks>
public sealed class GlyphOutline(float scale = 1f) : IHarfBuzzOutlineSink
{
    private readonly List<Contour> _contours = [];
    private Contour? _current;

    public void MoveTo(float x, float y)
    {
        ClosePath();
        _current = new Contour(Flip(x, y));
    }

    public void LineTo(float x, float y)
    {
        _current?.Segments.Add(new Segment(SegmentKind.Line, default, default, Flip(x, y)));
    }

    public void QuadraticTo(float controlX, float controlY, float x, float y)
    {
        _current?.Segments.Add(new Segment(SegmentKind.Quadratic, Flip(controlX, controlY), default, Flip(x, y)));
    }

    public void CubicTo(float control1X, float control1Y, float control2X, float control2Y, float x, float y)
    {
        _current?.Segments.Add(new Segment(SegmentKind.Cubic, Flip(control1X, control1Y),
            Flip(control2X, control2Y), Flip(x, y)));
    }

    public void ClosePath()
    {
        if (_current is null) return;
        if (_current.Segments.Count > 0) _contours.Add(_current);
        _current = null;
    }

    /// <summary>
    ///     The contours with the outer one counter-clockwise, reversing all of them when it is not.
    /// </summary>
    public IReadOnlyList<Contour> OrientedContours()
    {
        ClosePath();
        if (!OuterIsClockwise()) return _contours;

        var oriented = new List<Contour>(_contours.Count);
        foreach (var contour in _contours) oriented.Add(contour.Reversed());
        return oriented;
    }

    private bool OuterIsClockwise()
    {
        var outerArea = 0f;
        foreach (var contour in _contours)
        {
            var area = contour.SignedArea();
            if (MathF.Abs(area) > MathF.Abs(outerArea)) outerArea = area;
        }

        return outerArea < 0f;
    }

    private Vector2 Flip(float x, float y)
    {
        return new Vector2(x, -y) * scale;
    }
}
