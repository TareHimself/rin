using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Sdf;
using SixLabors.Fonts;
using Vector2 = System.Numerics.Vector2;
namespace rin.Framework.Views.Sdf;
using SdfExt = rin.Sdf;
public partial class TextRenderer : Disposable, IGlyphRenderer
{


    private Context _gen = new Context();
    
    public void BeginFigure()
    {
    }

    public void MoveTo(Vector2 point)
    {
        _gen.MoveTo(new Vec2<float>()
        {
            X = point.X,
            Y = point.Y
        });
    }

    public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
    {
        _gen.QuadraticBezierTo(new Vec2<float>()
            {
                X = secondControlPoint.X,
                Y = secondControlPoint.Y
            },
            new Vec2<float>()
            {
                X = point.X,
                Y = point.Y
            });
    }

    public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
    {
        _gen.CubicBezierTo(
            new Vec2<float>()
            {
                X = secondControlPoint.X,
                Y = secondControlPoint.Y
            },
            new Vec2<float>()
            {
                X = thirdControlPoint.X,
                Y = thirdControlPoint.Y
            },
            new Vec2<float>()
            {
                X = point.X,
                Y = point.Y
            }
            );
    }

    public void LineTo(Vector2 point)
    {
        _gen.LineTo(
            new Vec2<float>()
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
    public Result? Generate(float angleThreshold,float pixelRange)
    {
        return _gen.GenerateMtsdf(angleThreshold,pixelRange);
    }

    private static Vec2<float> ToNative(Vector2 v)
    {
        return new Vec2<float>(v.X, v.Y);
    }

    protected override void OnDispose(bool isManual)
    {
        _gen.Dispose();
    }
}