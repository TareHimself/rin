using System.Numerics;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Commands;

namespace experiment.StencilAndCover.Rendering;

public static class StencilAndCoverExtensions
{
    // Margin (screen pixels) added around the cover quad and each boundary AA strip so the
    // antialiasing ramp and small flattening error near the true curve are never clipped.
    private const float BoundaryMargin = 3f;

    public static CommandList FillPath(this CommandList list, ShapePath path, Matrix4x4 transform, Vector4 color)
    {
        var fanVertices = new List<Vector2>();
        var edges = new List<EdgeInstance>();
        var screenMin = new Vector2(float.MaxValue);
        var screenMax = new Vector2(float.MinValue);

        foreach (var contour in path.Contours)
        {
            var polygon = PathFlattener.Flatten(contour);
            FanTriangulator.EmitFan(polygon, fanVertices);

            foreach (var segment in contour.Segments)
            {
                var p0 = Vector2.Transform(segment.P0, transform);
                var control = Vector2.Transform(segment.Control, transform);
                var p2 = Vector2.Transform(segment.P2, transform);

                // An oriented quad hugging the chord, not an axis-aligned box around it — for a
                // long diagonal edge, an AABB of p0/control/p2 covers a huge swath of the shape's
                // interior and overlaps neighboring edges' boxes far more than the curve itself.
                var chord = p2 - p0;
                var chordLength = chord.Length();
                var tangent = chordLength > 1e-5f ? chord / chordLength : new Vector2(1f, 0f);
                var normal = new Vector2(-tangent.Y, tangent.X);

                // Perpendicular distance of the control point from the chord — how far a curved
                // segment bulges away from a straight line — so the quad still fully contains it.
                var toControl = control - p0;
                var bulge = MathF.Abs(tangent.X * toControl.Y - tangent.Y * toControl.X);

                var extend = tangent * BoundaryMargin;
                var widen = normal * (bulge + BoundaryMargin);

                var corner0 = p0 - extend - widen;
                var corner1 = p0 - extend + widen;
                var corner2 = p2 + extend - widen;
                var corner3 = p2 + extend + widen;

                edges.Add(new EdgeInstance
                {
                    P0 = p0,
                    Control = control,
                    P2 = p2,
                    Corner0 = corner0,
                    Corner1 = corner1,
                    Corner2 = corner2,
                    Corner3 = corner3,
                    Color = color
                });

                var boundsPad = new Vector2(BoundaryMargin);
                screenMin = Vector2.Min(screenMin, Vector2.Min(Vector2.Min(p0, control), p2) - boundsPad);
                screenMax = Vector2.Max(screenMax, Vector2.Max(Vector2.Max(p0, control), p2) + boundsPad);
            }
        }

        if (fanVertices.Count == 0) return list;

        var fillCommand = new StencilFillCommand { FanVertices = fanVertices, Transform = transform };
        list.Add(fillCommand);
        list.Add(new NoOpCommand());
        list.Add(new CoverCommand(fillCommand)
        {
            BoundsMin = screenMin,
            BoundsMax = screenMax,
            Edges = edges,
            Color = color
        });

        return list;
    }
}
