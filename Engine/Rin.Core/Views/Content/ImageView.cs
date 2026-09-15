using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Views.Graphics;
using Rin.Core.Shared.Math;
using Rin.Core.Views.Graphics.Quads;

namespace Rin.Core.Views.Content;


public record struct ImageInfo(ResourceHandle Handle, Extent2D Size);

/// <summary>
///     Draw's a 2D <see cref="ResourceHandle" /> if provided or a colored rectangle. Supports tint.
/// </summary>
public class ImageView : ContentView
{
    [PublicAPI]
    public ImageInfo Image
    {
        get;
        set
        {
            field = value;
            InvalidateDesiredSize();
            InvalidateLayout();
        }
    } = new ImageInfo(ResourceHandle.InvalidTexture,Extent2D.Zero);

    [PublicAPI] public Color Tint { get; set; } = new(1.0f);

    [PublicAPI] public Vector4 BorderRadius { get; set; } = new(0.0f);

    public override Vector2 ComputeDesiredContentSize()
    {
        var extent = Image.Size;
        return new Vector2(extent.Width, extent.Height);
    }

    // public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    // {
    //     //throw new NotImplementedException();
    //     if (TextureId != -1)
    //     {
    //         drawCommands.AddTexture(TextureId, info.Transform, GetContentSize(), Tint, null,
    //             BorderRadius);
    //     }
    // }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        if (Image.Handle != ResourceHandle.InvalidTexture) DrawImage(Image, transform, commands);
    }

    protected virtual void DrawImage(in ImageInfo imageId, Matrix4x4 transform, CommandList commands)
    {
        commands.AddTexture(imageId.Handle, transform, GetContentSize(), Tint, null,
            BorderRadius);
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        var size = GetDesiredContentSize();
        size = availableSpace.FiniteOr(size);
        return size.Clamp(new Vector2(0.0f), availableSpace);
    }
}