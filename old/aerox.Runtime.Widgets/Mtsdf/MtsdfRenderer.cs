using aerox.Msdf;
using aerox.Runtime.Math;
using SixLabors.Fonts;
using Vector2 = System.Numerics.Vector2;

namespace aerox.Runtime.Widgets.Mtsdf;
public partial class MtsdfRenderer : Disposable, IGlyphRenderer
{


    private Generator _gen = new Generator();
    
    public void BeginFigure()
    {
    }

    public void MoveTo(Vector2 point)
    {
        _gen.MoveTo(new Msdf.Vector2()
        {
            X = point.X,
            Y = point.Y
        });
    }

    public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
    {
        _gen.QuadraticBezierTo(new Msdf.Vector2()
            {
                X = secondControlPoint.X,
                Y = secondControlPoint.Y
            },
            new Msdf.Vector2()
            {
                X = point.X,
                Y = point.Y
            });
    }

    public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
    {
        _gen.CubicBezierTo(new Msdf.Vector2()
            {
                X = secondControlPoint.X,
                Y = secondControlPoint.Y
            },
            new Msdf.Vector2()
            {
                X = thirdControlPoint.X,
                Y = thirdControlPoint.Y
            },
            new Msdf.Vector2()
            {
                X = point.X,
                Y = point.Y
            });
    }

    public void LineTo(Vector2 point)
    {
        _gen.LineTo(
            new Msdf.Vector2()
            {
                X = point.X,
                Y = point.Y
            });
    }

    public void EndFigure()
    {
    }

    public void EndGlyph()
    {
    }

    public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
    {
        return true;
    }

    public void EndText()
    {
        _gen.End();
    }

    public void BeginText(in FontRectangle bounds)
    {
    }

    public TextDecorations EnabledDecorations()
    {
        return TextDecorations.None;
    }

    public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
    {
        throw new NotImplementedException();
    }
    // Renders into a 4 channel image
    public Generator.Result? Generate(int padding,float angleThreshold,float pixelRange)
    {
        return _gen.GenerateMtsdf(padding,angleThreshold,pixelRange);
    }

    private static Vector2<float> ToNative(Vector2 v)
    {
        return new Vector2<float>(v.X, v.Y);
    }

    protected override void OnDispose(bool isManual)
    {
        _gen.Dispose();
    }
}