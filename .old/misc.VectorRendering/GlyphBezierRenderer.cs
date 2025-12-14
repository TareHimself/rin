using System.Numerics;
using SixLabors.Fonts;

namespace misc.VectorRendering;

public class GlyphBezierRenderer : IGlyphRenderer
{
    private readonly List<Bezier> _curves = [];
    private readonly List<CurvePath> _paths = [];
    private Vector2 _point = Vector2.Zero;

    public void BeginFigure()
    {
        //throw new NotImplementedException();
    }

    public void MoveTo(Vector2 point)
    {
        _point = point;
    }

    public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
    {
        _curves.Add(new Bezier
        {
            Begin = _point,
            End = point,
            Control = secondControlPoint
        });
        _point = point;
    }

    public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
    {
        var p0 = _point;
        var p1 = secondControlPoint;
        var p2 = thirdControlPoint;
        var p3 = point;

        // Compute correct quadratic control points
        var q1 = 3f / 8f * p0 + 3f / 4f * p1 + 1f / 8f * p2;
        var q2 = 1f / 8f * p1 + 3f / 4f * p2 + 3f / 8f * p3;

        // First quadratic segment
        QuadraticBezierTo(q1, (p0 + 2f * p1) / 3f);

        // Second quadratic segment
        QuadraticBezierTo(q2, p3);

        // Update current position
        _point = p3;
    }

    public void LineTo(Vector2 point)
    {
        var control = (point + _point) / 2.0f;
        _curves.Add(new Bezier
        {
            Begin = _point,
            Control = control,
            End = point
        });
        _point = point;
    }

    public void EndFigure()
    {
        var z = 20;
        //throw new NotImplementedException();
    }

    public void EndGlyph()
    {
        _paths.Add(new CurvePath
        {
            Curves = _curves.ToArray()
        });

        _curves.Clear();
    }

    public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
    {
        _curves.Clear();
        return true;
    }

    public void EndText()
    {
        //throw new NotImplementedException();
    }

    public void BeginText(in FontRectangle bounds)
    {
        //throw new NotImplementedException();
    }

    public TextDecorations EnabledDecorations()
    {
        //throw new NotImplementedException();
        return new TextDecorations();
    }

    public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
    {
        //throw new NotImplementedException();
    }

    public IEnumerable<CurvePath> GetPaths()
    {
        return _paths;
    }

    public void Reset()
    {
        _curves.Clear();
        _paths.Clear();
        _point = Vector2.Zero;
    }
}