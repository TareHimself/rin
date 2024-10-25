using System.Runtime.InteropServices;
using aerox.Runtime.Graphics;
using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Graphics;
using aerox.Runtime.Widgets.Graphics.Quads;

namespace aerox.Runtime.Widgets.Content;

[StructLayout(LayoutKind.Sequential)]
internal struct ImageOptionsDeviceBuffer
{
    public Vector4<float> Tint;
    public int HasTexture;
    public Vector4<float> BorderRadius;
}

/// <summary>
///     Draw's a <see cref="Texture" /> if provided or a colored rectangle. Supports tint.
/// </summary>
public class Image : Widget
{

    private int _textureId;

    public Image()
    {
        _textureId = -1;
        Tint = new Color(1.0f);
    }

    public int TextureId
    {
        get => _textureId;
        set
        {
            _textureId = value;
            CheckSize();
        }
    }

    public Color Tint { get; set; }

    public Vector4<float> BorderRadius { get; set; } = new(0.0f);

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
    }
    
    protected override Size2d ComputeContentDesiredSize()
    {
        if (SGraphicsModule.Get().GetResourceManager()
                .GetTextureImage(TextureId) is { } texture)
        {
            return new Size2d
            {
                Width = texture.Extent.width,
                Height = texture.Extent.height
            };
        }

        return new Size2d();
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
                TextureId = TextureId
            }]));
        }
    }
}