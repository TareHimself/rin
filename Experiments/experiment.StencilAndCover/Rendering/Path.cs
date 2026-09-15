using System.Numerics;

namespace experiment.StencilAndCover.Rendering;

// One quadratic-Bezier edge of a contour. A straight line is stored with its control point at
// the segment's own chord midpoint — a valid degenerate quadratic that dist_to_bezier2 (see
// boundary_aa.slang) handles correctly without a separate line case.
public readonly record struct PathSegment(Vector2 P0, Vector2 Control, Vector2 P2);

public sealed class Contour
{
    public List<PathSegment> Segments { get; } = [];
}

// A Canvas-style path builder: MoveTo starts a new contour, Line/Quadratic/CubicTo append edges,
// Close connects back to the contour's start. Multiple contours in one Path are filled together
// under the nonzero winding rule, so an oppositely-wound inner contour becomes a hole.
public sealed class ShapePath
{
    private Vector2 _contourStart;
    private Vector2 _cursor;
    private Contour? _current;

    public List<Contour> Contours { get; } = [];

    public ShapePath MoveTo(Vector2 point)
    {
        _current = new Contour();
        Contours.Add(_current);
        _cursor = point;
        _contourStart = point;
        return this;
    }

    public ShapePath LineTo(Vector2 end)
    {
        AddSegment(end, (_cursor + end) * 0.5f);
        return this;
    }

    public ShapePath QuadraticTo(Vector2 control, Vector2 end)
    {
        AddSegment(end, control);
        return this;
    }

    // Degree-reduce the cubic to two quadratics (standard Tiller-Hanson midpoint split).
    public ShapePath CubicTo(Vector2 control1, Vector2 control2, Vector2 end)
    {
        var p0 = _cursor;
        var p3 = end;

        var q0 = p0 + 1.5f * (control1 - p0);
        var q1 = p3 + 1.5f * (control2 - p3);
        var mid = (q0 + q1) * 0.5f;

        QuadraticTo(q0, mid);
        QuadraticTo(q1, p3);
        return this;
    }

    public ShapePath Close()
    {
        if (Vector2.DistanceSquared(_cursor, _contourStart) > 1e-6f) LineTo(_contourStart);
        return this;
    }

    private void AddSegment(Vector2 end, Vector2 control)
    {
        _current!.Segments.Add(new PathSegment(_cursor, control, end));
        _cursor = end;
    }
}
