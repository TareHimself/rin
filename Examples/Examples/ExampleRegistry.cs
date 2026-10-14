using Examples.AssetViewer;
using Examples.AudioEffects;
using Examples.AudioPlayer;
using Examples.ChildWindows;
using Examples.Common;
using Examples.ImageStrip;
using Examples.NodeGraphTest;
using Examples.P2PChat;
using Examples.Paint;
using Examples.RenderGraphViewer;
using Examples.SceneTest;
using Examples.Sponza;
using Examples.StencilClip;
using Examples.TextInput;
using Examples.UiGallery;
using Examples.VideoPlayer;

namespace Examples;

public static class ExampleRegistry
{
    public static IReadOnlyList<Example> All { get; } =
    [
        new UiGalleryExample(),
        new PaintExample(),
        new VideoPlayerExample(),
        new AudioEffectsExample(),
        new ImageStripExample(),
        new StencilClipExample(),
        new ChildWindowsExample(),
        new NodeGraphExample(),
        new P2PChatExample(),
        new TextInputExample(),
        new AudioPlayerExample(),
        new SceneTestExample(),
        new SponzaExample(),
        new AssetViewerExample(),
        new RenderGraphViewerExample()
    ];

    public static Example? Find(string name)
    {
        foreach (var example in All)
            if (example.Name == name)
                return example;

        return null;
    }
}
