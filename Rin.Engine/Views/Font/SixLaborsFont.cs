using SixLabors.Fonts;

namespace Rin.Engine.Views.Font;

public class SixLaborsFont : IFont
{
    public SixLaborsFont(FontFamily font)
    {
        Family = font;
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
}