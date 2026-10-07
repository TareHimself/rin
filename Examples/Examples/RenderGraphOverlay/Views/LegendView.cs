using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;
using Examples.RenderGraphOverlay.Snapshot;

namespace Examples.RenderGraphOverlay.Views;

public class LegendView : ContentView
{
    private const float Height = 24f;
    private const float FontSize = 13f;
    private const float SwatchSize = 14f;
    private const float ItemGap = 22f;

    private static readonly (string Label, Color Color)[] Entries =
    [
        (nameof(PassCategory.Views), GraphPalette.ForCategory(PassCategory.Views)),
        (nameof(PassCategory.World), GraphPalette.ForCategory(PassCategory.World)),
        (nameof(PassCategory.Backend), GraphPalette.ForCategory(PassCategory.Backend)),
        (nameof(PassCategory.Core), GraphPalette.ForCategory(PassCategory.Core)),
        (nameof(PassCategory.App), GraphPalette.ForCategory(PassCategory.App)),
        ("Barrier", GraphPalette.Barrier)
    ];

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return new Vector2(Entries.Sum(e => ItemWidth(e.Label)) - ItemGap, Height);
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        var x = 0f;
        foreach (var (label, color) in Entries)
        {
            var swatch = Matrix4x4.Identity.Translate(new Vector2(x, 3f)).ChildOf(transform);
            commands.AddRect(swatch, new Vector2(SwatchSize), color, new Vector4(4f));

            var text = Matrix4x4.Identity.Translate(new Vector2(x + SwatchSize + 6f, 0f)).ChildOf(transform);
            commands.AddText(text, TextMeasure.FontName, label, FontSize, GraphPalette.Muted);
            x += ItemWidth(label);
        }
    }

    private static float ItemWidth(string label)
    {
        return SwatchSize + 6f + TextMeasure.Width(label, FontSize) + ItemGap;
    }
}
