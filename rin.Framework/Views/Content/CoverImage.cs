using System.Runtime.InteropServices;
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
    protected override Vec2<float> LayoutContent(Vec2<float> availableSpace)
    {
        return base.LayoutContent(availableSpace);
    }
    
    public override void CollectContent(Mat3 transform, DrawCommands drawCommands)
    {
        if (TextureId != -1)
        {
            var contentSize = GetContentSize();
            var fitSize = Fitter.ComputeCoverSize(contentSize, GetDesiredContentSize());
            var centerDist = (Vec2<float>)fitSize / 2.0f - (Vec2<float>)contentSize / 2.0f;
            var p1 = centerDist + 0.5f;
            var p2 = centerDist + contentSize;
            p1 /= fitSize;
            p2 /= fitSize;
            drawCommands.AddTexture(TextureId,transform, GetContentSize(), Tint, new Vec4<float>(p1, p2),
                BorderRadius);
        }
        else
        {
            base.CollectContent(transform, drawCommands);
        }
    }

}