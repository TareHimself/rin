using System.Numerics;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Quads;

namespace rin.Framework.Views.Content;

/// <summary>
///     Draw's a <see cref="Texture" /> if provided or a colored rectangle. Supports tint.
/// </summary>
public class CoverImage : Image
{
    protected override Vector2 LayoutContent(Vector2 availableSpace)
    {
        return base.LayoutContent(availableSpace);
    }

    public override void CollectContent(Mat3 transform, PassCommands commands)
    {
        if (TextureId != -1)
        {
            var contentSize = GetContentSize();
            var fitSize = Fitter.ComputeCoverSize(contentSize, GetDesiredContentSize());
            var centerDist = fitSize / 2.0f - contentSize / 2.0f;
            var p1 = centerDist + new Vector2(0.5f);
            var p2 = centerDist + contentSize;
            p1 /= fitSize;
            p2 /= fitSize;
            commands.AddTexture(TextureId, transform, GetContentSize(), Tint, new Vector4(p1.X, p1.Y, p2.X, p2.Y),
                BorderRadius);
        }
        else
        {
            base.CollectContent(transform, commands);
        }
    }
}