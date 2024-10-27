using System.Runtime.InteropServices;
using aerox.Runtime.Graphics;
using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Containers;
using aerox.Runtime.Widgets.Graphics;
using aerox.Runtime.Widgets.Graphics.Quads;

namespace aerox.Runtime.Widgets.Content;

/// <summary>
///     Draw's a <see cref="Texture" /> if provided or a colored rectangle. Supports tint.
/// </summary>
public class WCoverImage : WImage
{
    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        if (TextureId != -1)
        {
            var contentSize = GetContentSize();
            var fitSize = WCFitter.ComputeCoverSize(contentSize, GetDesiredContentSize());
            var centerDist = (Vector2<float>)fitSize / 2.0f - (Vector2<float>)contentSize / 2.0f;
            var p1 = centerDist;
            var p2 = centerDist + contentSize;
            p1 /= fitSize;
            p2 /= fitSize;
            
            drawCommands.Add(new QuadDrawCommand([new Quad(GetContentSize(), info.Transform)
            {
                TextureId = TextureId,
                Color = Tint,
                UV = new Vector4<float>(p1,p2),
                BorderRadius = BorderRadius
            }]));
        }
        else
        {
            base.CollectContent(info, drawCommands);
        }
    }
}