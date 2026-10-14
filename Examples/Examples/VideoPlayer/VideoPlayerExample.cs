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
        var view = context.Add(new VideoPlayerDemoView());
        // The arguments start with the example name, so take the first that is a file or a URL.
        if (context.Args.FirstOrDefault(IsVideoArgument) is { } media) view.Open(media);
    }

    private static bool IsVideoArgument(string argument)
    {
        return File.Exists(argument) ||
               (Uri.TryCreate(argument, UriKind.Absolute, out var url) && url.Scheme is "http" or "https");
    }
}
