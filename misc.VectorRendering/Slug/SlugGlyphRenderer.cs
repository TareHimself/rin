using System.Numerics;
using SixLabors.Fonts;

namespace misc.VectorRendering.Slug;

// A record pairing a set of normalized bezier curves with the glyph's layout bounds.
// "Normalized" means curve coordinates are glyph-relative (origin at bounds top-left),
// making the same curve data reusable when the glyph is rendered at different positions.
internal sealed record NormalizedGlyphPath(Bezier[] Curves, FontRectangle Bounds);

// SixLabors IGlyphRenderer that captures quadratic bezier outlines per glyph and
// normalizes them to glyph-local coordinates (origin at the character's bounds top-left).
//
// The normalization step is crucial for atlas reuse: a character like 'A' at screen
// position (100, 50) and (200, 300) uses the same bezier data, differing only by
// the position baked into the GlyphDrawData instance buffer.
internal sealed class SlugGlyphRenderer : IGlyphRenderer
{
    private readonly List<Bezier>             _curves = [];
    private readonly List<NormalizedGlyphPath> _paths  = [];
    private Vector2       _cursor      = Vector2.Zero;
    private Vector2       _figureStart = Vector2.Zero;
    private bool          _inFigure    = false;
    private FontRectangle _bounds      = default;

    public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
    {
        _curves.Clear();
        _bounds = bounds;
        return true;
    }

    // Mark the start of a new closed sub-path. The next MoveTo will record the
    // origin so we can emit a closing segment in EndFigure if needed.
    public void BeginFigure()
    {
        _inFigure = true;
    }

    public void MoveTo(Vector2 point)
    {
        _cursor = point;
        if (_inFigure)
        {
            _figureStart = point;
            _inFigure    = false;
        }
    }

    public void QuadraticBezierTo(Vector2 control, Vector2 end)
    {
        _curves.Add(new Bezier { Begin = _cursor, Control = control, End = end });
        _cursor = end;
    }

    public void CubicBezierTo(Vector2 c1, Vector2 c2, Vector2 end)
    {
        // Approximate cubic bezier with two quadratics (degree-reduction).
        // This is a simple midpoint split — adequate for font outlines since
        // TrueType uses quadratics natively and OpenType cubic glyphs are rare.
        var p0 = _cursor;
        var p3 = end;

        // Two-segment quadratic approximation following Tiller-Hanson
        var q1 = (3f * c1 - p0) / 2f;
        var q2 = (3f * c2 - p3) / 2f;
        var mid = (q1 + q2) / 2f;

        QuadraticBezierTo(q1, mid);
        QuadraticBezierTo(q2, p3);
    }

    public void LineTo(Vector2 end)
    {
        // Degenerate quadratic: control point at the midpoint of the line.
        var control = (_cursor + end) * 0.5f;
        _curves.Add(new Bezier { Begin = _cursor, Control = control, End = end });
        _cursor = end;
    }

    // SixLabors signals path closure here but does not always emit an explicit
    // closing LineTo. Without that segment the contour is open and the winding
    // number calculation loses the contribution of the closing edge.
    public void EndFigure()
    {
        if (Vector2.DistanceSquared(_cursor, _figureStart) > 1e-6f)
            LineTo(_figureStart);
    }

    public void EndGlyph()
    {
        if (_curves.Count == 0) return;

        // Normalize: subtract the glyph's layout origin so curves are in glyph-local space.
        // After this, a curve point (cx, cy) is at pixel offset (cx - 0, cy - 0) from the
        // glyph's top-left, and the atlas entry's BoundsMax = (Width, Height).
        var origin = new Vector2(_bounds.X, _bounds.Y);
        var normalized = _curves
            .Select(c => new Bezier
            {
                Begin   = c.Begin   - origin,
                Control = c.Control - origin,
                End     = c.End     - origin
            })
            .ToArray();

        // Rebuild bounds relative to the normalized origin.
        var localBounds = new FontRectangle(0, 0, _bounds.Width, _bounds.Height);
        _paths.Add(new NormalizedGlyphPath(normalized, localBounds));
        _curves.Clear();
    }

    public void BeginText(in FontRectangle bounds) { }
    public void EndText() { }

    public TextDecorations EnabledDecorations() => TextDecorations.None;
    public void SetDecoration(TextDecorations d, Vector2 start, Vector2 end, float thickness) { }

    // Returns one NormalizedGlyphPath per glyph contour group rendered so far.
    public IReadOnlyList<NormalizedGlyphPath> GetNormalizedPaths() => _paths;
}
