namespace Rin.Engine.Views.Font;

public interface IFont
{
    public string Name { get; }

    public float GetLineHeight(float fontSize);
    
    public IFontManager FontManager { get; }
    
    public GlyphRect[] MeasureText(in ReadOnlySpan<char> text, float size,
        float maxWidth = float.PositiveInfinity) => FontManager.MeasureText(this, text, size, maxWidth);
}