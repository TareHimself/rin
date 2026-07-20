using Rin.Core.Views.Font;
using Vector2 = System.Numerics.Vector2;

namespace Rin.Core.Views.Sdf;

/// <summary>
///     Consumes HarfBuzz glyph-outline draw callbacks and feeds them into <see cref="SdfBuilder" /> (a thin wrapper
///     around msdfgen - see native/Rin.Native/src/sdf.cpp), which follows the PostScript convention (solid contours
///     wind counter-clockwise, in the same Y-up space the shape is defined in).
///     <c>hb_font_draw_glyph</c> hands back contours in the font's native winding, unmodified, straight from its
///     outline table - TrueType (<c>glyf</c>) stores solid contours clockwise; PostScript-flavored OpenType
///     (<c>CFF</c>/<c>CFF2</c>) already matches msdfgen and stores them counter-clockwise (verified empirically:
///     NotoSans-Regular.ttf's outer contours have negative signed area, AdobeCleanUX-Regular.otf's have positive).
///     Negating Y (always, regardless of source table) both re-orients the glyph to match
///     <see cref="Content.TextBoxView.ComputeLayout" />'s own Y-flip when placing the quad, and - as an unavoidable
///     side effect of mirroring a single axis - reverses winding chirality. That side effect is exactly what
///     <c>glyf</c> contours need to match msdfgen, but it's the wrong direction for already-matching <c>CFF</c>
///     contours, so those need each contour's point order reversed on top of the Y-flip to cancel it back out.
/// </summary>
public class MtsdfTextRenderer(float scale = 1f, bool reverseWinding = false) : IDisposable, IHarfBuzzOutlineSink
{
    private readonly SdfBuilder _gen = new();
    private readonly List<Segment> _segments = [];
    private Vector2 _contourStart;

    public void Dispose()
    {
        _gen.Dispose();
    }

    public void MoveTo(float x, float y)
    {
        if (!reverseWinding)
        {
            _gen.BeginContour();
            _gen.MoveTo(Flip(x, y));
            return;
        }

        _contourStart = Flip(x, y);
        _segments.Clear();
    }

    public void LineTo(float x, float y)
    {
        if (!reverseWinding)
        {
            _gen.LineTo(Flip(x, y));
            return;
        }

        _segments.Add(new Segment { Kind = SegmentKind.Line, To = Flip(x, y) });
    }

    public void QuadraticTo(float controlX, float controlY, float x, float y)
    {
        if (!reverseWinding)
        {
            _gen.QuadraticBezierTo(Flip(controlX, controlY), Flip(x, y));
            return;
        }

        _segments.Add(new Segment { Kind = SegmentKind.Quadratic, Control1 = Flip(controlX, controlY), To = Flip(x, y) });
    }

    public void CubicTo(float control1X, float control1Y, float control2X, float control2Y, float x, float y)
    {
        if (!reverseWinding)
        {
            _gen.CubicBezierTo(Flip(control1X, control1Y), Flip(control2X, control2Y), Flip(x, y));
            return;
        }

        _segments.Add(new Segment
        {
            Kind = SegmentKind.Cubic, Control1 = Flip(control1X, control1Y), Control2 = Flip(control2X, control2Y),
            To = Flip(x, y)
        });
    }

    public void ClosePath()
    {
        if (!reverseWinding)
        {
            _gen.EndContour();
            return;
        }

        // Replay this contour in reverse (same shape, opposite winding) to cancel out the flip caused by Flip().
        _gen.BeginContour();
        _gen.MoveTo(_segments.Count > 0 ? _segments[^1].To : _contourStart);

        for (var i = _segments.Count - 1; i >= 0; i--)
        {
            var segment = _segments[i];
            var to = i == 0 ? _contourStart : _segments[i - 1].To;
            switch (segment.Kind)
            {
                case SegmentKind.Line:
                    _gen.LineTo(to);
                    break;
                case SegmentKind.Quadratic:
                    _gen.QuadraticBezierTo(segment.Control1, to);
                    break;
                case SegmentKind.Cubic:
                    _gen.CubicBezierTo(segment.Control2, segment.Control1, to);
                    break;
            }
        }

        _gen.EndContour();
    }

    private Vector2 Flip(float x, float y)
    {
        return new Vector2(x, -y) * scale;
    }

    // Renders into a 4 channel image
    public SdfResult? Generate(float angleThreshold, float pixelRange)
    {
        _gen.Finish();
        return _gen.GenerateMTSDF(angleThreshold, pixelRange);
    }

    private enum SegmentKind
    {
        Line,
        Quadratic,
        Cubic
    }

    private struct Segment
    {
        public SegmentKind Kind;
        public Vector2 Control1;
        public Vector2 Control2;
        public Vector2 To;
    }
}
