using SixLabors.Fonts;

namespace Rin.Framework.Views.Font;

public class SixLaborsFont : IFont
{
    public SixLaborsFont(FontFamily font, IFontManager fontManager)
    {
        
        Family = font;
        FontManager = fontManager;
        font.TryGetMetrics(FontStyle.Bold, out var metrics);
    }


    public FontFamily Family { get; }

    public string Name => Family.Name;

    public float GetLineHeight(float fontSize)
    {
        var font = Family.CreateFont(fontSize);
        return font.FontMetrics is { } metrics
            ? metrics.HorizontalMetrics.AdvanceHeightMax * 64 / metrics.ScaleFactor * fontSize
            : 0;
    }

    public GlyphRect[] MeasureText(in ReadOnlySpan<char> text, float size, float maxWidth = Single.PositiveInfinity)
    {
       return FontManager.MeasureText(this, text, size, maxWidth);
    }

    public IEnumerable<CodePoint> GetCodePoints()
    {
        if (Family.TryGetMetrics(FontStyle.Regular, out var metrics))
        {
            return metrics.GetAvailableCodePoints().Select(c => new CodePoint(c.Value));
        }
        return [];
    }

    public IFontManager FontManager { get; }
}