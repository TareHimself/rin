using System.Runtime.InteropServices;
using rin.Graphics;
using rin.Core.Math;
using rin.Widgets.Enums;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Quads;

namespace rin.Widgets.Content;

/// <summary>
///     Draw's a <see cref="Texture" /> if provided or a colored rectangle. Supports tint.
/// </summary>
public class Image : ContentWidget
{

    private int _textureId = -1;

    public Image()
    {
    }

    public int TextureId
    {
        get => _textureId;
        set
        {
            _textureId = value;
            Invalidate(InvalidationType.DesiredSize);
        }
    }

    public Color Tint { get; set; } = new Color(1.0f);

    public Vector4<float> BorderRadius { get; set; } = new(0.0f);

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
    }
    
    protected override Vector2<float> ComputeDesiredContentSize()
    {
        if (SGraphicsModule.Get().GetResourceManager()
                .GetTextureImage(TextureId) is { } texture)
        {
            return new Vector2<float>
            {
                X = texture.Extent.width,
                Y = texture.Extent.height
            };
        }

        return new Vector2<float>();
    }
    
    protected override void OnRemovedFromSurface(Surface surface)
    {
        base.OnRemovedFromSurface(surface);
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

    public override void CollectContent(Matrix3 transform, DrawCommands drawCommands)
    {
        if (TextureId != -1)
        {
            drawCommands.AddTexture(TextureId,transform, GetContentSize(), Tint, null,
                BorderRadius);
        }
    }

    protected override Vector2<float> LayoutContent(Vector2<float> availableSpace)
    {
        var size = GetDesiredContentSize();
        size.X = availableSpace.X.FiniteOr(size.X);
        size.Y = availableSpace.Y.FiniteOr(size.Y);
        
        return size.Clamp(new Vector2<float>(0.0f), availableSpace);
    }
}