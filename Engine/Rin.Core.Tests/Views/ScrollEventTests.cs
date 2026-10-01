using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;

namespace Rin.Core.Tests.Views;

public class ScrollEventTests
{
    [SetUp]
    public void SetUp()
    {
        IApplication.Override = new FakeApplication();
    }

    [TearDown]
    public void TearDown()
    {
        IApplication.Override = null;
    }

    [Test]
    public void ScrollEventOverThePointerReachesTheContentViewUnderIt()
    {
        var root = new RootView();
        var content = new ScrollSpyView();
        root.Add(content);
        root.Layout(new Vector2(800f, 600f), true);

        var scroll = new ScrollSurfaceEvent(new FakeSurface(), new Vector2(400f, 300f), new Vector2(0f, -1f));
        root.HandleEvent(scroll, root.GetLocalTransform());

        Assert.That(content.Received, Is.EqualTo(new[] { new Vector2(0f, -1f) }));
        Assert.That(scroll.Target, Is.SameAs(content));
    }

    [Test]
    public void ScrollEventOutsideTheViewDoesNotReachIt()
    {
        var root = new RootView();
        var content = new ScrollSpyView();
        root.Add(content);
        root.Layout(new Vector2(800f, 600f), true);

        var scroll = new ScrollSurfaceEvent(new FakeSurface(), new Vector2(900f, 300f), new Vector2(0f, -1f));
        root.HandleEvent(scroll, root.GetLocalTransform());

        Assert.That(content.Received, Is.Empty);
    }

    private sealed class ScrollSpyView : ContentView
    {
        public List<Vector2> Received { get; } = [];

        protected override Vector2 LayoutContent(in Vector2 availableSpace)
        {
            return availableSpace;
        }

        public override void CollectContent(in Matrix4x4 transform, CommandList commands)
        {
        }

        protected override bool OnScroll(ScrollSurfaceEvent e)
        {
            Received.Add(e.Delta);
            return true;
        }
    }
}
