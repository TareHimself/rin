using System.Runtime.InteropServices;
using rin.Graphics;
using rin.Core.Math;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Quads;

namespace rin.Widgets.Content;

/// <summary>
///     Draw's a <see cref="Texture" /> if provided or a colored rectangle. Supports tint.
/// </summary>
public class ImageWidget : Widget
{

    private int _textureId = -1;

    public ImageWidget()
    {
    }

    public int TextureId
    {
        get => _textureId;
        set
        {
            _textureId = value;
            TryUpdateDesiredSize();
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

    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        //throw new NotImplementedException();
        if (TextureId != -1)
        {
            drawCommands.Add(new QuadDrawCommand([new Quad(GetContentSize(), info.Transform)
            {
                TextureId = TextureId,
                Color = Tint,
                BorderRadius = BorderRadius
            }]));
        }
    }
}