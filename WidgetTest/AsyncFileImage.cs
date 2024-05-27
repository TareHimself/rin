using System.Runtime.InteropServices;
using aerox.Runtime.Extensions;
using aerox.Runtime.Graphics;
using aerox.Runtime.Widgets;
using aerox.Runtime.Widgets.Defaults.Content;
using aerox.Runtime.Widgets.Events;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Vulkan;

namespace WidgetTest;

public class AsyncFileImage : Image
{
    private float _alpha = 0.0f;
    private float _alphaTarget;


    public AsyncFileImage()
    {
    }

    public AsyncFileImage(string filePath) : this()
    {
        Task.Run(() => LoadFile(filePath));
    }

    public AsyncFileImage(string filePath, Action<AsyncFileImage> loadCallback) : this()
    {
        Task.Run(() => LoadFile(filePath).Then(() => loadCallback.Invoke(this)));
    }

    private async Task LoadFile(string filePath)
    {
        using var imgData = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(filePath);
        var imgRawData = new byte[imgData.Width * imgData.Height * Marshal.SizeOf<Rgba32>()];
        imgData.CopyPixelDataTo(imgRawData);
        Texture = new Texture(imgRawData, new VkExtent3D
            {
                width = (uint)imgData.Width,
                height = (uint)imgData.Height,
                depth = 1
            },
            ImageFormat.Rgba8,
            ImageFilter.Linear,
            ImageTiling.Repeat);

        // We do this since the image adds a ref to the texture
        Texture.Dispose();
    }

    // public override void Draw(WidgetFrame frame, DrawInfo info)
    // {
    //     // _alpha = MathUtils.InterpolateTo(_alpha, _alphaTarget, (float)Runtime.Instance.GetLastDeltaSeconds(), 0.8f);
    //     //
    //     // var sin = (float)Math.Sin(Runtime.Instance.GetTimeSinceCreation() * 1.3f);
    //     // var borderRadius = float.Abs(sin) * 100.0f;
    //     // BorderRadius = 100.0f * _alpha;
    //     base.Draw(frame, info);
    // }

    public override void Draw(WidgetFrame frame, DrawInfo info)
    {
        // var sin = (float)Math.Sin(Runtime.Instance.GetTimeSinceCreation());
        // var borderRadius = float.Abs(sin) * 150.0f;
        // BorderRadius = borderRadius;
        base.Draw(frame, info);
    }

    protected override void OnCursorEnter(CursorMoveEvent e)
    {
        base.OnCursorEnter(e);
        _alphaTarget = 1.0f;
    }

    protected override void OnCursorLeave(CursorMoveEvent e)
    {
        base.OnCursorLeave(e);
        _alphaTarget = 0.0f;
    }


    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
    }
}