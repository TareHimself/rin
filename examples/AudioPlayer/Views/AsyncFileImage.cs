using rin.Framework.Core;
using rin.Framework.Core.Extensions;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Views;
using rin.Framework.Views.Content;
using rin.Framework.Views.Events;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Quads;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Vulkan;

namespace AudioPlayer.Views;

public class AsyncFileImage : CoverImage
{
    private float _alpha = 0.0f;
    private float _alphaTarget;
    private CancellationTokenSource _token = new CancellationTokenSource();
    public AsyncFileImage(string filePath) : base()
    {
        Task.Run(() => LoadFile(filePath),_token.Token);
    }

    public AsyncFileImage(string filePath, Action<AsyncFileImage> loadCallback) : base()
    {
        Task.Run(() => LoadFile(filePath),_token.Token).Then(() => loadCallback.Invoke(this)).ConfigureAwait(false);
    }

    private async Task LoadFile(string filePath)
    {
        using var imgData = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(filePath);
        using var buffer = imgData.ToBuffer();
        await SGraphicsModule.Get().GetTextureManager().CreateTexture(buffer,
            new Extent3D
            {
                Width = (uint)imgData.Width,
                Height = (uint)imgData.Height
            },
            ImageFormat.RGBA8).Then(c => TextureId = c);
    }

    // public override void Draw(ViewFrame frame, DrawInfo info)
    // {
    //     // _alpha = MathUtils.InterpolateTo(_alpha, _alphaTarget, (float)Runtime.Instance.GetLastDeltaSeconds(), 0.8f);
    //     //
    //     // var sin = (float)Math.Sin(Runtime.Instance.GetTimeSinceCreation() * 1.3f);
    //     // var borderRadius = float.Abs(sin) * 100.0f;
    //     // BorderRadius = 100.0f * _alpha;
    //     base.Draw(frame, info);
    // }

    protected override Vec2<float> LayoutContent(Vec2<float> availableSpace)
    {
        if (TextureId == -1)
        {
            return availableSpace;
        }

        return base.LayoutContent(availableSpace);
    }

    public override void CollectContent(Mat3 transform, PassCommands commands)
    {
        
        if (TextureId == -1)
        {
            var opacity = (float)Math.Abs(Math.Sin(SRuntime.Get().GetTimeSeconds() * 4.0f)) * 0.7f; 
            commands.AddRect(transform, GetContentSize(),
                color: new Vec4<float>(new Vec3<float>(0.8f),opacity),BorderRadius);
        }
        else
        {
            base.CollectContent(transform, commands);
        }
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
        _token.Cancel();
        base.OnDispose(isManual);
    }
}