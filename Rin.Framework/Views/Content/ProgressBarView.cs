using System.Numerics;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Math;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics.Quads;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Content;

/// <summary>
///     Simple progress bar implementation
/// </summary>
public class ProgressBarView(Func<float> getProgress,Action<float>? onClick = null) : ContentView
{
    public Color BackgroundColor { get; set; } = Color.Red;
    public Color ForegroundColor { get; set; } = Color.White;
    public Vector4 BorderRadius { get; set; }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return availableSpace;
    }
    
    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        var a = ComputeAbsoluteContentTransform();
        var b = transform;
        var z = a == b;
        var size = GetContentSize();
        commands.AddRect(transform, size, BackgroundColor, BorderRadius);
        commands.AddRect(transform, size * new Vector2(float.Clamp(_pendingProgress ?? getProgress(), 0.0f, 1.0f), 1.0f), ForegroundColor,
            BorderRadius);
        // using var libvlc = new LibVLC(enableDebugLogs: true);
        // using var media = new Media(libvlc, new Uri(@"C:\tmp\big_buck_bunny.mp4"));
        // using var mediaplayer = new MediaPlayer(media);
        //
        // mediaplayer.Play();
    }

    public override bool OnCursorDown(CursorDownSurfaceEvent e)
    {
        if (e.Button is CursorButton.One && onClick is not null)
        {
            var transform = ComputeAbsoluteContentTransform();
            var localPosition = e.Position.Transform(transform.Inverse());
            _pendingProgress = localPosition.X / Size.X;
            
            return true;
        }
        return base.OnCursorDown(e);
    }

    private float? _pendingProgress = null;
    protected override bool OnCursorMove(CursorMoveSurfaceEvent e)
    {
        if (_pendingProgress is not null && onClick is not null)
        {
            var transform = ComputeAbsoluteContentTransform();
            var localPosition = e.Position.Transform(transform.Inverse());
            _pendingProgress = localPosition.X / Size.X;
            return true;
        }
        return base.OnCursorMove(e);
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        if (_pendingProgress is not null && onClick is not null)
        {
            var transform = ComputeAbsoluteContentTransform();
            var localPosition = e.Position.Transform(transform.Inverse());
            _pendingProgress = localPosition.X / Size.X;
            onClick(_pendingProgress.Value);
            _pendingProgress = null;
        }
        
        base.OnCursorUp(e);
    }
}