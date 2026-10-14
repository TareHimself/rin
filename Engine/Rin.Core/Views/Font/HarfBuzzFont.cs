namespace Rin.Core.Views.Font;

public class HarfBuzzFont : IFont, IDisposable
{
    private readonly IntPtr _face;
    private readonly IntPtr _font;
    private bool _disposed;

    internal HarfBuzzFont(IntPtr face, IntPtr font, string name, uint unitsPerEm, IFontManager fontManager)
    {
        _face = face;
        _font = font;
        Name = name;
        UnitsPerEm = unitsPerEm;
        FontManager = fontManager;
    }

    internal IntPtr Face => _face;
    internal IntPtr Font => _font;
    internal uint UnitsPerEm { get; }

    public string Name { get; }

    public IFontManager FontManager { get; }

    public float GetLineHeight(float fontSize)
    {
        if (!HarfBuzzNative.TryGetHorizontalFontExtents(_font, out var extents) || UnitsPerEm == 0) return 0;
        return (extents.Ascender - extents.Descender + extents.LineGap) * fontSize / UnitsPerEm;
    }

    public GlyphRect[] MeasureText(in ReadOnlySpan<char> text, float size, float maxWidth = float.PositiveInfinity)
    {
        return FontManager.MeasureText(this, text, size, maxWidth);
    }

    public IEnumerable<CodePoint> GetCodePoints()
    {
        return HarfBuzzNative.EnumerateCodepoints(_face).Select(codepoint => new CodePoint((int)codepoint));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        HarfBuzzNative.hb_font_destroy(_font);
        HarfBuzzNative.hb_face_destroy(_face);
    }
}
