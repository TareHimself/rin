using System.Numerics;
using JetBrains.Annotations;
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

    [PublicAPI]
    public int TextureId
    {
        get => _textureId;
        set
        {
            _textureId = value;
            Invalidate(InvalidationType.DesiredSize);
        }
    }

    [PublicAPI] public Color Tint { get; set; } = new(1.0f);

    [PublicAPI] public Vector4 BorderRadius { get; set; } = new(0.0f);

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
    }

    protected override Vector2 ComputeDesiredContentSize()
    {
        if (SGraphicsModule.Get().GetTextureManager()
                .GetTextureImage(TextureId) is { } texture)
            return new Vector2
            {
                X = texture.Extent.Width,
                Y = texture.Extent.Height
            };

        return new Vector2();
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

    public override void CollectContent(Mat3 transform, PassCommands commands)
    {
        if (TextureId != -1)
            commands.AddTexture(TextureId, transform, GetContentSize(), Tint, null,
                BorderRadius);
    }

    protected override Vector2 LayoutContent(Vector2 availableSpace)
    {
        var size = GetDesiredContentSize();
        size = availableSpace.FiniteOr(size);
        return size.Clamp(new Vector2(0.0f), availableSpace);
    }
}