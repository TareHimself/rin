using System.Runtime.InteropServices;
using rin.Core.Extensions;
using rin.Graphics;
using rin.Widgets;
using rin.Widgets.Content;
using rin.Widgets.Events;
using rin.Widgets.Graphics;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Vulkan;

namespace WidgetTest;

public class AsyncFileImage : ImageWidget
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
        using var buffer = imgData.ToBuffer(); 
        TextureId = SGraphicsModule.Get().GetResourceManager().CreateTexture(buffer,
            new VkExtent3D
            {
                width = (uint)imgData.Width,
                height = (uint)imgData.Height,
                depth = 1
            },
            ImageFormat.Rgba8);
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

    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        // var sin = (float)Math.Sin(Runtime.Instance.GetTimeSinceCreation());
        // var borderRadius = float.Abs(sin) * 150.0f;
        // BorderRadius = borderRadius;
        base.CollectContent(info, drawCommands);
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