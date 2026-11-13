using Rin.Core.Shared.Pooling;
using Rin.Core.Views;
using Rin.Core.Views.Font;

namespace Examples.RenderGraphOverlay.Views;

public static class TextMeasure
{
    private const int MaxStackGlyphs = 64;

    public static string FontName => IViewsModule.Get().DefaultFontFamily;

    public static float Width(string text, float fontSize)
    {
        if (IViewsModule.Get().FontManager.GetFont(FontName) is not { } font) return text.Length * fontSize * 0.6f;

        using var glyphs = new ScratchSpan<GlyphRect>(text.Length,
            text.Length <= MaxStackGlyphs ? stackalloc GlyphRect[text.Length] : default);
        var count = font.MeasureText(text, fontSize, glyphs.Span);
        var width = 0f;
        foreach (var glyph in glyphs.Span[..count]) width = float.Max(width, glyph.PenX + glyph.Advance);
        return width;
    }
}
