using System.Numerics;
using Rin.Core;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Views;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;

namespace rin.Examples.Common.Views;

public class AsyncFileImageView : CoverImageView
{
    private readonly CancellationTokenSource _token = new();
    private float _alpha = 0.0f;
    private float _alphaTarget;

    public AsyncFileImageView(string filePath)
    {
        Task.Run(() => LoadFile(filePath), _token.Token);
    }

    public AsyncFileImageView(string filePath, Action<AsyncFileImageView> loadCallback)
    {
        Task.Run(() => LoadFile(filePath), _token.Token).DispatchMain(() => loadCallback.Invoke(this));
    }

    private async Task LoadFile(string filePath)
    {
        using var image = HostImage.Create(File.OpenRead(filePath));
        await image.CreateTexture(out var handle);
        await IApplication.Get().MainDispatcher.Enqueue(() => ImageHandle = handle);
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        if (!ImageHandle.IsValid()) return availableSpace;

        return base.LayoutContent(availableSpace);
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        if (!ImageHandle.IsValid())
        {
            var opacity = (float)Math.Abs(Math.Sin(IApplication.Get().TimeSeconds * 4.0f)) * 0.7f;
            commands.AddRect(transform, GetContentSize(), new Color(0.8f, opacity), BorderRadius);
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