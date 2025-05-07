namespace Rin.Engine.Views.Font;

public interface IFontManager : IDisposable
{
    /// <summary>
    ///     Begin loading the textures for these characters
    /// </summary>
    /// <param name="font"></param>
    /// <param name="characters"></param>
    public Task Prepare(IFont font, IEnumerable<char> characters);

    /// <summary>
    ///     Begin loading the textures for these characters and insert all characters into one atlas
    /// </summary>
    /// <param name="font"></param>
    /// <param name="characters"></param>
    public Task PrepareAtlas(IFont font, IEnumerable<char> characters);

    /// <summary>
    ///     Try load all system fonts
    /// </summary>
    public void LoadSystemFonts();

    /// <summary>
    ///     Try load a font from a stream
    /// </summary>
    public void LoadFont(Stream fileStream);

    /// <summary>
    ///     Returns the <see cref="LiveGlyphInfo" /> Generated at the size it was generated at
    /// </summary>
    /// <param name="font">The font</param>
    /// <param name="character">The character to get the glyph of</param>
    /// <returns></returns>
    public LiveGlyphInfo GetGlyph(IFont font, char character);

    public IFont? GetFont(string name);

    public GlyphRect[] MeasureText(IFont font, in ReadOnlySpan<char> text, float size,
        float maxWidth = float.PositiveInfinity);

    public float GetPixelRange();
}