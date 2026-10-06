using Examples.AssetViewer;
using Examples.AudioPlayer;
using Examples.Common;
using Examples.NodeGraphTest;
using Examples.P2PChat;
using Examples.RenderGraphViewer;
using Examples.SceneTest;
using Examples.Sponza;
using Examples.UiGallery;
using Examples.ViewsTest;

namespace Examples;

public static class ExampleRegistry
{
    public static IReadOnlyList<Example> All { get; } =
    [
        new UiGalleryExample(),
        new ViewsTestExample(),
        new NodeGraphExample(),
        new P2PChatExample(),
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
