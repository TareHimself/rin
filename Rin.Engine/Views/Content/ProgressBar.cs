using System.Numerics;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Quads;

namespace Rin.Engine.Views.Content;

/// <summary>
///     Simple progress bar implementation
/// </summary>
public class ProgressBar(Func<float> getProgress) : ContentView
{
    public Color BackgroundColor { get; set; } = Color.Red;
    public Color ForegroundColor { get; set; } = Color.White;
    public Vector4 BorderRadius { get; set; }

    protected override Vector2 LayoutContent(Vector2 availableSpace)
    {
        return availableSpace;
    }

    protected override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2();
    }

    public override void CollectContent(Matrix4x4 transform, CommandList commands)
    {
        var size = GetContentSize();
        commands.AddRect(transform, size, BackgroundColor, BorderRadius);
        commands.AddRect(transform, size * new Vector2(float.Clamp(getProgress(), 0.0f, 1.0f), 1.0f), ForegroundColor,
            BorderRadius);
        // using var libvlc = new LibVLC(enableDebugLogs: true);
        // using var media = new Media(libvlc, new Uri(@"C:\tmp\big_buck_bunny.mp4"));
        // using var mediaplayer = new MediaPlayer(media);
        //
        // mediaplayer.Play();
    }
}