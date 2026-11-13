namespace Rin.Core.Views.Font;

public interface IFont
{
    public string Name { get; }

    public IFontManager FontManager { get; }

    public float GetLineHeight(float fontSize);

    /// <summary>
    ///     Writes one rect per character into <paramref name="into" /> and returns how many were written.
    /// </summary>
    public int MeasureText(in ReadOnlySpan<char> text, float size, Span<GlyphRect> into,
        float maxWidth = float.PositiveInfinity);

    public IEnumerable<CodePoint> GetCodePoints();
}