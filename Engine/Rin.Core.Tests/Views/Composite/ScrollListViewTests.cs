using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Events;
using Rin.Core.Graphics;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Layouts;

namespace Rin.Core.Tests.Views.Composite;

public class ScrollListViewTests
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
    public void MaxScrollIsTheContentLengthBeyondTheGivenSpace()
    {
        var list = MakeList(Axis.Column, 100f, 100f, 100f);

        list.Layout(new Vector2(200f, 150f));

        Assert.That(list.GetMaxScroll(), Is.EqualTo(150f));
        Assert.That(list.IsScrollable(), Is.True);
    }

    [Test]
    public void ContentThatFitsIsNotScrollable()
    {
        var list = MakeList(Axis.Column, 100f, 100f);

        list.Layout(new Vector2(200f, 300f));

        Assert.That(list.GetMaxScroll(), Is.EqualTo(0f));
        Assert.That(list.IsScrollable(), Is.False);
    }

    [Test]
    public void RowAxisScrollsHorizontally()
    {
        var list = MakeList(Axis.Row, 100f, 100f, 100f);

        list.Layout(new Vector2(150f, 50f));

        Assert.That(list.GetMaxScroll(), Is.EqualTo(150f));
    }

    [Test]
    public void ScrollingMovesContentThatStartedOutOfViewIntoView()
    {
        var list = MakeList(Axis.Column, 100f, 100f, 100f);
        list.Layout(new Vector2(200f, 150f));
        var lastItem = list.GetSlots()[2].Child;
        var viewHeight = list.GetContentSize().Y;

        Assert.That(ScreenTop(list, lastItem), Is.GreaterThanOrEqualTo(viewHeight));

        list.ScrollTo(list.GetMaxScroll());

        Assert.That(ScreenTop(list, lastItem), Is.EqualTo(viewHeight - 100f));
        Assert.That(ScreenTop(list, lastItem) + 100f, Is.LessThanOrEqualTo(viewHeight));
    }

    [Test]
    public void ScrollToClampsToTheScrollableRange()
    {
        var list = MakeList(Axis.Column, 100f, 100f, 100f);
        list.Layout(new Vector2(200f, 150f));

        list.ScrollTo(1000f);
        Assert.That(list.GetScroll(), Is.EqualTo(150f));

        list.ScrollTo(-1000f);
        Assert.That(list.GetScroll(), Is.EqualTo(0f));
    }

    [Test]
    public void ScrollByReportsWhetherTheOffsetMoved()
    {
        var list = MakeList(Axis.Column, 100f, 100f, 100f);
        list.Layout(new Vector2(200f, 150f));

        Assert.That(list.ScrollBy(50f), Is.True);
        Assert.That(list.GetScroll(), Is.EqualTo(50f));

        Assert.That(list.ScrollBy(1000f), Is.True);
        Assert.That(list.GetScroll(), Is.EqualTo(150f));

        Assert.That(list.ScrollBy(50f), Is.False);
        Assert.That(list.GetScroll(), Is.EqualTo(150f));
    }

    [Test]
    public void ScrollingAtTheTopEdgeReportsNoMovement()
    {
        var list = MakeList(Axis.Column, 100f, 100f, 100f);
        list.Layout(new Vector2(200f, 150f));

        Assert.That(list.ScrollBy(-10f), Is.False);
    }

    [Test]
    public void WheelOverTheListScrollsItAndMarksTheEventHandled()
    {
        var root = new RootView();
        var list = MakeList(Axis.Column, 100f, 100f, 100f);
        root.Add(list);
        root.Layout(new Vector2(200f, 150f), true);
        var scroll = new ScrollSurfaceEvent(new FakeSurface(), new Vector2(10f, 10f), new Vector2(0f, -20f));

        root.HandleEvent(scroll, root.GetLocalTransform());

        Assert.That(list.GetScroll(), Is.EqualTo(20f));
        Assert.That(scroll.Target, Is.SameAs(list));
    }

    [Test]
    public void ShrinkingContentPullsTheOffsetBackIntoRange()
    {
        var list = MakeList(Axis.Column, 100f, 100f, 100f);
        list.Layout(new Vector2(200f, 150f));
        list.ScrollTo(list.GetMaxScroll());

        list.Layout(new Vector2(200f, 280f));

        Assert.That(list.GetMaxScroll(), Is.EqualTo(20f));
        Assert.That(list.GetScroll(), Is.EqualTo(20f));
    }

    [Test]
    public void ContentThatNoLongerOverflowsResetsTheOffset()
    {
        var list = MakeList(Axis.Column, 100f, 100f, 100f);
        list.Layout(new Vector2(200f, 150f));
        list.ScrollTo(list.GetMaxScroll());

        list.Layout(new Vector2(200f, 500f));

        Assert.That(list.GetScroll(), Is.EqualTo(0f));
    }

    [TestCase(0f, new[] { 0, 1 })]
    [TestCase(110f, new[] { 1, 2 })]
    [TestCase(250f, new[] { 2, 3 })]
    public void CollectsExactlyTheItemsInsideTheViewportWhateverTheScroll(float scroll, int[] expectedItems)
    {
        var items = new[] { new FixedView(200f, 100f), new FixedView(200f, 100f), new FixedView(200f, 100f), new FixedView(200f, 100f) };
        var list = new ScrollListView();
        foreach (var item in items) list.Add(item);
        list.Layout(new Vector2(200f, 150f));
        list.ScrollTo(scroll);
        var clip = new Rect2D(new Vector2(-10000f), new Vector2(20000f));

        list.Collect(Matrix4x4.Identity, clip, new CommandList { SurfaceSize = new Vector2(800f, 600f) });

        var collected = Enumerable.Range(0, items.Length).Where(i => items[i].CollectCount > 0);
        Assert.That(collected, Is.EqualTo(expectedItems));
    }

    private static ScrollListView MakeList(Axis axis, params float[] itemLengths)
    {
        var list = new ScrollListView { Axis = axis };
        foreach (var length in itemLengths)
            list.Add(axis == Axis.Column ? new FixedView(200f, length) : new FixedView(length, 50f));
        return list;
    }

    private static float ScreenTop(ScrollListView list, IView child)
    {
        return child.Offset.Y + list.GetLocalContentTransform().Translation.Y;
    }

    private sealed class FixedView(float width, float height) : ContentView
    {
        protected override Vector2 LayoutContent(in Vector2 availableSpace)
        {
            return new Vector2(width, height);
        }

        public int CollectCount { get; private set; }

        public override void CollectContent(in Matrix4x4 transform, CommandList commands)
        {
            CollectCount++;
        }
    }
}
