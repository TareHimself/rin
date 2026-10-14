using Rin.Core.Views.Font;
using Rin.Core.Views.Mtsdf;

namespace Rin.Core.Views.Sdf;

/// <summary>
///     Consumes HarfBuzz glyph-outline draw callbacks through a <see cref="GlyphOutline" /> and feeds the oriented
///     contours into <see cref="MtsdfBuilder" /> (a thin wrapper around msdfgen - see native/Rin.Native/src/sdf.cpp).
///     The outline negates Y so the glyph matches the Y-flip <see cref="Content.TextBoxView.ComputeLayout" /> applies
///     when placing the quad, and orients the contours from their geometry, so any font renders right side out.
/// </summary>
public class MtsdfTextRenderer(float scale = 1f) : IDisposable, IHarfBuzzOutlineSink
{
    private readonly MtsdfBuilder _gen = new();
    private readonly GlyphOutline _outline = new(scale);

    public void Dispose()
    {
        _gen.Dispose();
    }

    public void MoveTo(float x, float y)
    {
        _outline.MoveTo(x, y);
    }

    public void LineTo(float x, float y)
    {
        _outline.LineTo(x, y);
    }

    public void QuadraticTo(float controlX, float controlY, float x, float y)
    {
        _outline.QuadraticTo(controlX, controlY, x, y);
    }

    public void CubicTo(float control1X, float control1Y, float control2X, float control2Y, float x, float y)
    {
        _outline.CubicTo(control1X, control1Y, control2X, control2Y, x, y);
    }

    public void ClosePath()
    {
        _outline.ClosePath();
    }

    // Renders into a 4 channel image
    public SdfResult? Generate(float angleThreshold, float pixelRange)
    {
        foreach (var contour in _outline.OrientedContours())
        {
            _gen.BeginContour();
            _gen.MoveTo(contour.Start);
            foreach (var segment in contour.Segments)
                switch (segment.Kind)
                {
                    case SegmentKind.Line:
                        _gen.LineTo(segment.To);
                        break;
                    case SegmentKind.Quadratic:
                        _gen.QuadraticBezierTo(segment.Control1, segment.To);
                        break;
                    case SegmentKind.Cubic:
                        _gen.CubicBezierTo(segment.Control1, segment.Control2, segment.To);
                        break;
                }

            _gen.EndContour();
        }

        _gen.Finish();
        return _gen.GenerateMTSDF(angleThreshold, pixelRange);
    }
}
