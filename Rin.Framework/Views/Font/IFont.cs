namespace Rin.Framework.Views.Font;

public interface IFont
{
    public string Name { get; }

    public IFontManager FontManager { get; }

    public float GetLineHeight(float fontSize);

    public GlyphRect[] MeasureText(in ReadOnlySpan<char> text, float size,
        float maxWidth = float.PositiveInfinity)
    {
        return FontManager.MeasureText(this, text, size, maxWidth);
    }
}