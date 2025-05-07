using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Textures;
using Rin.Engine.Math;
using Rin.Engine.Views.Enums;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Quads;

namespace Rin.Engine.Views.Content;

/// <summary>
///     Draw's a <see cref="Texture" /> if provided or a colored rectangle. Supports tint.
/// </summary>
public class Image : ContentView
{
    private TextureHandle _textureId = TextureHandle.InvalidImage;

    [PublicAPI]
    public TextureHandle TextureId
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

    protected override Vector2 ComputeDesiredContentSize()
    {
        if (SGraphicsModule.Get().GetTextureFactory()
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

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        if (TextureId.IsValid()) DrawImage(TextureId, transform, commands);
    }

    protected virtual void DrawImage(in TextureHandle textureId, Matrix4x4 transform, CommandList commands)
    {
        commands.AddTexture(textureId, transform, GetContentSize(), Tint, null,
            BorderRadius);
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        var size = GetDesiredContentSize();
        size = availableSpace.FiniteOr(size);
        return size.Clamp(new Vector2(0.0f), availableSpace);
    }
}