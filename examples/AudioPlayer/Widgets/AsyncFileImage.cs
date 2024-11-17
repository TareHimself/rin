using System.Runtime.InteropServices;
using rin.Core.Extensions;
using rin.Graphics;
using rin.Widgets;
using rin.Widgets.Content;
using rin.Widgets.Events;
using rin.Widgets.Graphics;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Vulkan;

namespace AudioPlayer.Widgets;

public class AsyncFileImage : Image
{
    private float _alpha = 0.0f;
    private float _alphaTarget;
    

    public AsyncFileImage(string filePath) : base()
    {
        Task.Run(() => LoadFile(filePath));
    }

    public AsyncFileImage(string filePath, Action<AsyncFileImage> loadCallback) : base()
    {
        Task.Run(() => LoadFile(filePath).Then(() => loadCallback.Invoke(this)));
    }

    private async Task LoadFile(string filePath)
    {
        using var imgData = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(filePath);
        using var imgRawData = imgData.ToBuffer();
        await SGraphicsModule.Get().GetResourceManager().CreateTexture(imgRawData,
            new VkExtent3D
            {
                width = (uint)imgData.Width,
                height = (uint)imgData.Height,
                depth = 1
            },
            ImageFormat.Rgba8).Then(c => TextureId = c);
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