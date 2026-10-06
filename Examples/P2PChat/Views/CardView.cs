using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;

namespace P2PChat.Views;

/// <summary>
///     A rounded rect with an optional 1px border and a soft shadow built from stacked translucent rects.
/// </summary>
public class CardView : RectView
{
    private const int ShadowLayers = 5;
    private const float ShadowSpread = 5f;

    public Color? BorderColor { get; set; }
    public float ShadowAlpha { get; set; }

    protected override void CollectSelf(Matrix4x4 transform, CommandList cmds)
    {
        var size = GetSize();
        if (ShadowAlpha > 0f) CollectShadow(transform, size, cmds);

        if (BorderColor is { } border)
            cmds.AddRect(transform.Translate(new Vector2(-1f)), size + new Vector2(2f), border, BorderRadius + new Vector4(1f));

        cmds.AddRect(transform, size, Color, BorderRadius);
    }

    private void CollectShadow(Matrix4x4 transform, Vector2 size, CommandList cmds)
    {
        for (var layer = ShadowLayers; layer >= 1; layer--)
        {
            var grow = layer * ShadowSpread;
            var shadow = new Color(0f, 0f, 0f, ShadowAlpha / ShadowLayers);
            cmds.AddRect(transform.Translate(new Vector2(-grow, -grow + ShadowSpread * 2f)), size + new Vector2(grow * 2f),
                shadow, BorderRadius + new Vector4(grow));
        }
    }
}
