using Rin.Core.Extensions;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared.Buffers;
using Rin.Core.Shared.Video;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.CommandHandlers;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.PassConfigs;
using Rin.Core.Shared.Math;
using Rin.Core.Views.Graphics.Quads;
using Rin.Core.Views.Graphics.Shaders;
using Rin.Shade;
using Rin.Core.Shared.Pooling;

namespace Rin.Core.Views.Content;

public class VideoPlayerView : ContentView
{
    private readonly IVideoPlayer _player;
    private bool _laidOutForVideo;

    private VideoPlayerView(IVideoPlayer context)
    {
        _player = context;
    }

    public bool IsReady => _player.IsReady;
    public bool HasFailed => _player.HasFailed;
    public Exception? Error => _player.Error;
    public bool HasVideo => _player.HasVideo;
    public bool IsPlaying => _player.IsPlaying;
    public double Position => _player.Position;
    public double Duration => _player.Duration;

    public void Play()
    {
        _player.Play();
    }

    public void Pause()
    {
        _player.Pause();
    }

    public override void Dispose()
    {
        base.Dispose();
        _player.Dispose();
    }

    public static VideoPlayerView FromFile(string fileName)
    {
        return FromSource(new FileVideoSource(fileName));
    }

    public static VideoPlayerView FromUrl(Uri url)
    {
        return FromSource(new HttpVideoSource(url));
    }

    public static VideoPlayerView FromSource(IVideoSource source)
    {
        var player = new WebmVideoPlayer();
        player.SetSource(source);
        return new VideoPlayerView(player);
    }


    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return availableSpace.FiniteOr(Vector2.Zero);
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        if (_player.HasVideo)
        {
            var extent = _player.VideoExtent;
            return new Vector2(extent.Width, extent.Height);
        }

        return Vector2.Zero;
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        if (_player.HasVideo)
        {
            var buff = _player.CopyRecentFrame();
            commands.Add(new VideoCommand(transform, GetContentSize(), buff, _player.VideoExtent));
        }

        // A live stream has no duration, so there is no progress to show.
        if (_player.Duration <= 0.0) return;

        const int barSize = 10;
        var contentSize = GetContentSize();
        var barOffset = contentSize with { X = 0 };
        barOffset.Y -= barSize;
        var barTransform = transform.ApplyBefore(Matrix4x4.Identity.Translate(barOffset));

        commands.AddRect(barTransform,
            new Vector2(contentSize.X * (float)(_player.Position / _player.Duration), barSize), Color.White);
        commands.AddRect(barTransform,
            new Vector2(contentSize.X * (float)(_player.DecodedPosition / _player.Duration), barSize),
            Color.White with { A = 0.5f });
    }

    public override void OnCursorMove(CursorMoveSurfaceEvent e, in Matrix4x4 transform)
    {
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        _player.TryDecode();
        // The extent is unknown until the headers are read, which happens on the decode thread.
        if (_laidOutForVideo || !_player.IsReady) return;
        _laidOutForVideo = true;
        InvalidateDesiredSize();
        InvalidateLayout();
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        if (e.Button is CursorButton.One)
        {
            if (_player.IsPlaying)
                _player.Pause();
            else
                _player.Play();
            e.Target = this;
            return;
        }

        if (e.Button is CursorButton.Two)
        {
            var localPosition = e.Position.Transform(transform.Inverse());
            var size = GetSize();
            var percent = localPosition.X / size.X;
            if (_player.Duration <= 0.0) return;
            _player.Seek(_player.Duration * percent);
            e.Target = this;
        }
    }
}
