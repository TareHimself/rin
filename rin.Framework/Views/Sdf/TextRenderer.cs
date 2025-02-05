using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Sdf;
using SixLabors.Fonts;
using Vector2 = System.Numerics.Vector2;
namespace rin.Framework.Views.Sdf;
using SdfExt = rin.Sdf;
public class TextRenderer : IDisposable, IGlyphRenderer
{
    private Context _gen = new Context();
    
    public void BeginFigure()
    {
    }

    public void MoveTo(Vector2 point)
    {
        _gen.MoveTo(point);
    }

    public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
    {
        _gen.QuadraticBezierTo(secondControlPoint,point);
    }

    public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
    {
        _gen.CubicBezierTo(secondControlPoint,thirdControlPoint,point);
    }

    public void LineTo(Vector2 point)
    {
        _gen.LineTo(point);
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
    
    public void Dispose()
    {
        _gen.Dispose();
    }
}