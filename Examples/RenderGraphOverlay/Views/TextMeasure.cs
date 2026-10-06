using Rin.Core.Views;

namespace RenderGraphOverlay.Views;

public static class TextMeasure
{
    public const string FontName = "Noto Sans";

    public static float Width(string text, float fontSize)
    {
        if (IViewsModule.Get().FontManager.GetFont(FontName) is not { } font) return text.Length * fontSize * 0.6f;

        var glyphs = font.MeasureText(text, fontSize);
        return glyphs.Length == 0 ? 0f : glyphs.Max(g => g.PenX + g.Advance);
    }
}
