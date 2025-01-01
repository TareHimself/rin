using System.Runtime.InteropServices;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Quads;

namespace rin.Framework.Views.Content;

/// <summary>
///     Draw's a <see cref="Texture" /> if provided or a colored rectangle. Supports tint.
/// </summary>
public class Image : ContentView
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

    public Vec4<float> BorderRadius { get; set; } = new(0.0f);

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
    }
    
    protected override Vec2<float> ComputeDesiredContentSize()
    {
        if (SGraphicsModule.Get().GetResourceManager()
                .GetTextureImage(TextureId) is { } texture)
        {
            return new Vec2<float>
            {
                X = texture.Extent.width,
                Y = texture.Extent.height
            };
        }

        return new Vec2<float>();
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

    public override void CollectContent(Mat3 transform, DrawCommands drawCommands)
    {
        if (TextureId != -1)
        {
            drawCommands.AddTexture(TextureId,transform, GetContentSize(), Tint, null,
                BorderRadius);
        }
    }

    protected override Vec2<float> LayoutContent(Vec2<float> availableSpace)
    {
        var size = GetDesiredContentSize();
        size.X = availableSpace.X.FiniteOr(size.X);
        size.Y = availableSpace.Y.FiniteOr(size.Y);
        
        return size.Clamp(new Vec2<float>(0.0f), availableSpace);
    }
}