using System.Runtime.InteropServices;
using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Defaults.Content;

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
    private readonly MaterialInstance _materialInstance;
    private readonly DeviceBuffer _optionsBuffer;
    private Vector4<float> _borderRadius = new(0.0f);
    private bool _optionsDirty;
    private Texture? _texture;
    private bool _textureDirty;
    private Color _tint;

    public Image()
    {
        var gs = Runtime.Instance.GetModule<GraphicsModule>();
        _optionsBuffer = gs.GetAllocator()
            .NewUniformBuffer<ImageOptionsDeviceBuffer>(debugName: "Image Options Buffer");
        _texture = null;
        _optionsDirty = true;
        _tint = new Color(1.0f);
        _materialInstance = WidgetsModule.CreateMaterial(
            gs.LoadShader(@"D:\Github\vengine\aerox.Runtime\shaders\2d\rect.vert"),
            gs.LoadShader(@"D:\Github\vengine\aerox.Runtime\shaders\2d\image.frag"));
        _materialInstance.BindBuffer("opts", _optionsBuffer);
    }

    public Texture? Texture
    {
        get => _texture;
        set
        {
            
            value?.Reserve();
            _texture?.Dispose();
            _texture = value;
            CheckSize();
            MarkTextureDirty();
        }
    }

    public Color Tint
    {
        get => _tint;
        set
        {
            _tint = value;
            MarkOptionsDirty();
        }
    }

    public Vector4<float> BorderRadius
    {
        get => _borderRadius;
        set
        {
            _borderRadius = value;
            MarkOptionsDirty();
        }
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        _materialInstance.Dispose();
        _optionsBuffer.Dispose();
        _texture?.Dispose();
    }

    private void MarkOptionsDirty()
    {
        _optionsDirty = true;
    }

    private void MarkTextureDirty()
    {
        _textureDirty = true;
    }

    public override Size2d ComputeDesiredSize()
    {
        if (_texture == null) return new Size2d();

        var size = _texture.GetSize();

        return new Size2d
        {
            Width = size.width,
            Height = size.height
        };
    }

    protected override void OnAddedToRoot(WidgetSurface widgetSurface)
    {
        base.OnAddedToRoot(widgetSurface);
        _materialInstance.BindBuffer("ui", widgetSurface.GlobalBuffer);
    }

    protected override void OnRemovedFromRoot(WidgetSurface widgetSurface)
    {
        base.OnRemovedFromRoot(widgetSurface);
    }

    public override void Draw(WidgetFrame frame, DrawInfo info)
    {
        if (_optionsDirty || _textureDirty)
        {
            _optionsBuffer.Write(new ImageOptionsDeviceBuffer
            {
                Tint = _tint,
                HasTexture = _texture == null ? 0 : 1,
                BorderRadius = _borderRadius
            });
            _optionsDirty = false;
        }

        if (_textureDirty)
        {
            if (_texture != null) _materialInstance.BindTexture("ImageT", _texture);

            _textureDirty = false;
        }

        frame.AddMaterialRect(_materialInstance,info);
    }
}