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
            PassCategory.Views => Color.FromRgb(0.22f, 0.42f, 0.82f),
            PassCategory.World => Color.FromRgb(0.2f, 0.62f, 0.38f),
            PassCategory.Backend => Color.FromRgb(0.85f, 0.5f, 0.18f),
            PassCategory.Core => Color.FromRgb(0.6f, 0.35f, 0.8f),
            _ => Color.FromRgb(0.2f, 0.62f, 0.66f)
        };
    }

    public static Color ForResource(uint resourceId)
    {
        var hue = resourceId * 0.61803398875f % 1f;
        return Color.FromHsv(hue, 0.55f, 0.95f);
    }
}
