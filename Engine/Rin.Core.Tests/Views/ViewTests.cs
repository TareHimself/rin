using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Graphics;

namespace Rin.Core.Tests.Views;

public class ViewTests : FakeApplicationFixture
{
    [Test]
    public void BoxToSurfaceAddsEveryParentsPaddingAndScroll()
    {
        var (_, list, leaf) = PaddedScrolledTree();

        list.ScrollTo(50f);

        Assert.That(leaf.BoxToSurface().Translation, Is.EqualTo(new Vector3(15f, 165f, 0f)));
    }

    [Test]
    public void AViewIsDrawnWhereBoxToSurfaceSays()
    {
        var (root, list, leaf) = PaddedScrolledTree();
        list.ScrollTo(list.GetMaxScroll());
        var clip = new Rect2D(new Vector2(-10000f), new Vector2(20000f));

        root.Collect(root.BoxToParent(), clip, new CommandList { SurfaceSize = new Vector2(800f, 600f) });

        Assert.That(leaf.DrawnAt, Is.EqualTo(leaf.ContentToSurface()));
    }

    // A 10-padded sizer holding a 5-padded scroll list of three 100-tall items; the leaf is the last item.
    private static (SizerView Root, ScrollListView List, RecordingView Leaf) PaddedScrolledTree()
    {
        var leaf = new RecordingView();
        var list = new ScrollListView { Padding = new Padding(5f) };
        list.Add(new RecordingView());
        list.Add(new RecordingView());
        list.Add(leaf);
        var root = new SizerView { Padding = new Padding(10f), InitChild = list };
        root.Layout(new Vector2(220f, 170f));
        return (root, list, leaf);
    }

    private sealed class RecordingView : ContentView
    {
        public Matrix4x4? DrawnAt { get; private set; }

        protected override Vector2 LayoutContent(in Vector2 availableSpace)
        {
            return new Vector2(100f, 100f);
        }

        public override void CollectContent(in Matrix4x4 transform, CommandList commands)
        {
            DrawnAt = transform;
        }
    }
}
