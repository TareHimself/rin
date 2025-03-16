using System.Numerics;
using Rin.Engine.Core;
using Rin.Engine.Core.Extensions;
using Rin.Engine.Core.Math;
using Rin.Engine.Graphics;
using Rin.Engine.Views;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Quads;
using SixLabors.ImageSharp.PixelFormats;

namespace rin.Examples.Common.Views;

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
        TextureId = SGraphicsModule.Get().CreateTexture(buffer,
            new Extent3D
            {
                Width = (uint)imgData.Width,
                Height = (uint)imgData.Height,
            },
            ImageFormat.RGBA8);
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

    protected override Vector2 LayoutContent(Vector2 availableSpace)
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
            var opacity = (float)Math.Abs(Math.Sin(SEngine.Get().GetTimeSeconds() * 4.0f)) * 0.7f;
            commands.AddRect(transform, GetContentSize(),
                color: new Vector4(new Vector3(0.8f),opacity),BorderRadius);
        }
        else
        {
            base.CollectContent(transform, commands);
        }
    }

    protected override void OnCursorEnter(CursorMoveSurfaceEvent e)
    {
        base.OnCursorEnter(e);
        _alphaTarget = 1.0f;
    }

    protected override void OnCursorLeave()
    {
        base.OnCursorLeave();
        _alphaTarget = 0.0f;
    }


    public override void Dispose()
    {
        _token.Cancel();
    }
}