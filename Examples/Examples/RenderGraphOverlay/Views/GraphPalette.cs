using Rin.Core.Views;
using Examples.RenderGraphOverlay.Snapshot;

namespace Examples.RenderGraphOverlay.Views;

public static class GraphPalette
{
    public static readonly Color Barrier = new(0.32f, 0.34f, 0.4f, 1f);
    public static readonly Color Muted = new(0.6f, 0.63f, 0.7f, 1f);

    public static Color ForCategory(PassCategory category)
    {
        return category switch
        {
            PassCategory.Views => new Color(0.22f, 0.42f, 0.82f, 1f),
            PassCategory.World => new Color(0.2f, 0.62f, 0.38f, 1f),
            PassCategory.Backend => new Color(0.85f, 0.5f, 0.18f, 1f),
            PassCategory.Core => new Color(0.6f, 0.35f, 0.8f, 1f),
            _ => new Color(0.2f, 0.62f, 0.66f, 1f)
        };
    }

    public static Color ForResource(uint resourceId)
    {
        var hue = resourceId * 0.61803398875f % 1f;
        return FromHsv(hue, 0.55f, 0.95f);
    }

    private static Color FromHsv(float hue, float saturation, float value)
    {
        var sector = hue * 6f;
        var fraction = sector - float.Floor(sector);
        var p = value * (1f - saturation);
        var q = value * (1f - fraction * saturation);
        var t = value * (1f - (1f - fraction) * saturation);
        return ((int)sector % 6) switch
        {
            0 => new Color(value, t, p, 1f),
            1 => new Color(q, value, p, 1f),
            2 => new Color(p, value, t, 1f),
            3 => new Color(p, q, value, 1f),
            4 => new Color(t, p, value, 1f),
            _ => new Color(value, p, q, 1f)
        };
    }
}
