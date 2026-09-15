using System.Numerics;

namespace experiment.Slug.Rendering;

// One quadratic Bezier segment of a vector outline: C(t) = (1-t)^2*P0 + 2t(1-t)*P1 + t^2*P2.
// Every contour SLUG rasterizes is a closed loop of these — straight lines are represented by
// duplicating the end point as the control point ({p1, p2, p2}), per the Slug README's tip on
// encoding lines without a separate curve type.
public readonly record struct QuadraticBezier(Vector2 P0, Vector2 P1, Vector2 P2)
{
    public static QuadraticBezier Line(Vector2 from, Vector2 to) => new(from, to, to);

    public Vector2 Min => Vector2.Min(P0, Vector2.Min(P1, P2));
    public Vector2 Max => Vector2.Max(P0, Vector2.Max(P1, P2));

    // A curve is degenerate on an axis when all three control points share that coordinate —
    // it can never contribute a crossing to a ray cast along that axis, per the Slug README:
    // flat horizontal lines must never enter horizontal bands, flat vertical lines never enter
    // vertical bands.
    public bool IsFlatX => P0.X == P1.X && P1.X == P2.X;
    public bool IsFlatY => P0.Y == P1.Y && P1.Y == P2.Y;
}
