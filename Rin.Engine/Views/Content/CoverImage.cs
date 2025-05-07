using System.Numerics;
using Rin.Engine.Graphics.Textures;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Quads;

namespace Rin.Engine.Views.Content;

/// <summary>
///     Draw's a <see cref="Texture" /> if provided or a colored rectangle. Supports tint.
/// </summary>
public class CoverImage : Image
{
    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return base.LayoutContent(availableSpace);
    }

    protected override void DrawImage(in TextureHandle textureId, Matrix4x4 transform, CommandList commands)
    {
        var contentSize = GetContentSize();
        var fitSize = Fitter.ComputeCoverSize(contentSize, GetDesiredContentSize());
        var centerDist = fitSize / 2.0f - contentSize / 2.0f;
        var p1 = centerDist + new Vector2(0.5f);
        var p2 = centerDist + contentSize;
        p1 /= fitSize;
        p2 /= fitSize;
        commands.AddTexture(textureId, transform, GetContentSize(), Tint, new Vector4(p1.X, p1.Y, p2.X, p2.Y),
            BorderRadius);
    }
}