using Examples.VideoPlayer.Views;
using Rin.Core.Graphics;

namespace Examples.VideoPlayer;

public sealed class VideoPlayerExample : Example
{
    public override string Name => "video-player";

    public override string Title => "Video Player";

    public override Extent2D WindowSize => new(1280, 720);

    public override void Start(ExampleContext context)
    {
        context.Add(new VideoPlayerDemoView());
    }
}
