﻿using System.Numerics;
using Rin.Engine;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.Views;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Quads;

namespace rin.Examples.Common.Views;

public class AsyncFileImage : CoverImage
{
    private readonly CancellationTokenSource _token = new();
    private float _alpha = 0.0f;
    private float _alphaTarget;

    public AsyncFileImage(string filePath)
    {
        Task.Run(() => LoadFile(filePath), _token.Token);
    }

    public AsyncFileImage(string filePath, Action<AsyncFileImage> loadCallback)
    {
        Task.Run(() => LoadFile(filePath), _token.Token).DispatchMain(() => loadCallback.Invoke(this));
    }

    private async Task LoadFile(string filePath)
    {
        using var image = HostImage.Create(File.OpenRead(filePath)); //await Image.LoadAsync<Rgba32>(filePath);
        var (handle,task) = image.CreateTexture();
        await task;
        await SEngine.Get().GetMainDispatcher().Enqueue(() => ImageId = handle);
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

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        if (!ImageId.IsValid()) return availableSpace;

        return base.LayoutContent(availableSpace);
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        if (!ImageId.IsValid())
        {
            var opacity = (float)Math.Abs(Math.Sin(SEngine.Get().GetTimeSeconds() * 4.0f)) * 0.7f;
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