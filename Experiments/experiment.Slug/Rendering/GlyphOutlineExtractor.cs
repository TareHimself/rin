using System.Numerics;
using SixLabors.Fonts;

namespace experiment.Slug.Rendering;

// Captures one glyph's outline as quadratic Bezier contours, normalized to glyph-local
// (em) space with the origin at the glyph's own layout bounds top-left. Normalizing here is
// what lets SlugAtlas cache one packed shape per (font, size, codepoint) and reuse it at any
// screen position.
//
// SixLabors.Fonts is used purely as an offline outline source — this has nothing to do with
// Rin's own (HarfBuzz-based) text system; SLUG only needs raw glyph geometry.
public sealed class GlyphOutlineExtractor : IGlyphRenderer
{
    private readonly List<QuadraticBezier> _curves = [];
    private Vector2 _cursor;
    private Vector2 _figureStart;
    private FontRectangle _bounds;

    public IReadOnlyList<QuadraticBezier> Curves => _curves;
    public FontRectangle Bounds => _bounds;

    public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
    {
        _curves.Clear();
        _bounds = bounds;
        return true;
    }

    public void BeginFigure() { }

    public void MoveTo(Vector2 point)
    {
        _cursor = point;
        _figureStart = point;
    }

    public void QuadraticBezierTo(Vector2 control, Vector2 end)
    {
        _curves.Add(new QuadraticBezier(_cursor, control, end));
        _cursor = end;
    }

    // Degree-reduce a cubic to two quadratics (standard Tiller-Hanson midpoint split).
    // TrueType glyph data is already quadratic; this only matters for the rare OpenType/CFF
    // (cubic) outline.
    public void CubicBezierTo(Vector2 control1, Vector2 control2, Vector2 end)
    {
        var p0 = _cursor;
        var p3 = end;

        var q0 = p0 + 1.5f * (control1 - p0);
        var q1 = p3 + 1.5f * (control2 - p3);
        var mid = (q0 + q1) * 0.5f;

        QuadraticBezierTo(q0, mid);
        QuadraticBezierTo(q1, p3);
    }

    // Per the Slug README: encode a straight line as a quadratic with the second control
    // point duplicated as the end point.
    public void LineTo(Vector2 end)
    {
        _curves.Add(QuadraticBezier.Line(_cursor, end));
        _cursor = end;
    }

    public void EndFigure()
    {
        // SixLabors doesn't always emit an explicit closing segment; without one the contour
        // is open and its closing edge is missing from the winding-number test.
        if (Vector2.DistanceSquared(_cursor, _figureStart) > 1e-6f)
            LineTo(_figureStart);
    }

    public void EndGlyph() { }

    public void BeginText(in FontRectangle bounds) { }
    public void EndText() { }

    public TextDecorations EnabledDecorations() => TextDecorations.None;
    public void SetDecoration(TextDecorations decorations, Vector2 start, Vector2 end, float thickness) { }

    // Returns the captured contours normalized so the glyph's own bounds top-left is the origin.
    public VectorPath GetNormalizedPath()
    {
        if (_curves.Count == 0) return new VectorPath([], Vector2.Zero, Vector2.Zero);

        var origin = new Vector2(_bounds.X, _bounds.Y);
        var normalized = _curves
            .Select(c => new QuadraticBezier(c.P0 - origin, c.P1 - origin, c.P2 - origin))
            .ToArray();

        return VectorPath.FromCurves(normalized);
    }
}
