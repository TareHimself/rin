using SixLabors.Fonts;

namespace rin.Framework.Views.Font;

public interface IFontManager : IDisposable
{

    /// <summary>
    /// Begin loading the textures for these characters
    /// </summary>
    /// <param name="fontFamily"></param>
    /// <param name="characters"></param>
    /// <param name="style"></param>
    public void Prepare(FontFamily fontFamily,ReadOnlySpan<char> characters,FontStyle style = FontStyle.Regular);
    
    /// <summary>
    /// Try load all system fonts
    /// </summary>
    public void LoadSystemFonts();
    
    /// <summary>
    /// Try load a font from a stream
    /// </summary>
    public void LoadFont(Stream fileStream);

    /// <summary>
    /// Returns the <see cref="GlyphInfo"/> Generated at the size it was generated at
    /// </summary>
    /// <param name="font">The font</param>
    /// <param name="character">The character to get the glyph of</param>
    /// <returns></returns>
    public GlyphInfo GetGlyph(SixLabors.Fonts.Font font,char character);
    
    public bool TryGetFont(string name,out FontFamily family);

    public float GetPixelRange();
}