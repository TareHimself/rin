using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Layouts;

namespace Rin.Core.Tests.Views.Composite;

public class WrapListViewTests
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
    public void RowItemsThatFitStayOnOneLine()
    {
        var items = new[] { new FixedView(100f, 40f), new FixedView(100f, 40f), new FixedView(100f, 40f) };
        var size = LayOut(Axis.Row, new Vector2(400f, 1000f), items);

        Assert.That(items.Select(i => i.Offset), Is.EqualTo(new[] { new Vector2(0f, 0f), new Vector2(100f, 0f), new Vector2(200f, 0f) }));
        Assert.That(size, Is.EqualTo(new Vector2(300f, 40f)));
    }

    [Test]
    public void RowItemsWrapOntoTheNextLineBelowTheTallestItem()
    {
        var items = new[] { new FixedView(100f, 40f), new FixedView(100f, 60f), new FixedView(100f, 20f) };
        var size = LayOut(Axis.Row, new Vector2(250f, 1000f), items);

        Assert.That(items.Select(i => i.Offset), Is.EqualTo(new[] { new Vector2(0f, 0f), new Vector2(100f, 0f), new Vector2(0f, 60f) }));
        Assert.That(size, Is.EqualTo(new Vector2(200f, 80f)));
    }

    [Test]
    public void ItemWiderThanTheLineIsPlacedAloneWithoutAnEmptyLineBeforeIt()
    {
        var items = new[] { new FixedView(500f, 40f), new FixedView(100f, 40f) };
        LayOut(Axis.Row, new Vector2(300f, 1000f), items);

        Assert.That(items.Select(i => i.Offset), Is.EqualTo(new[] { new Vector2(0f, 0f), new Vector2(0f, 40f) }));
    }

    [Test]
    public void ColumnItemsFlowDownAndWrapIntoTheNextColumn()
    {
        var items = new[] { new FixedView(40f, 100f), new FixedView(60f, 100f), new FixedView(20f, 100f) };
        var size = LayOut(Axis.Column, new Vector2(1000f, 250f), items);

        Assert.That(items.Select(i => i.Offset), Is.EqualTo(new[] { new Vector2(0f, 0f), new Vector2(0f, 100f), new Vector2(60f, 0f) }));
        Assert.That(size, Is.EqualTo(new Vector2(80f, 200f)));
    }

    [Test]
    public void UnboundedMainAxisKeepsEverythingOnOneLine()
    {
        var items = new[] { new FixedView(100f, 40f), new FixedView(100f, 40f) };
        LayOut(Axis.Row, new Vector2(float.PositiveInfinity, 1000f), items);

        Assert.That(items[1].Offset, Is.EqualTo(new Vector2(100f, 0f)));
    }

    private static Vector2 LayOut(Axis axis, Vector2 space, FixedView[] items)
    {
        var layout = new WrapListLayout(axis, new WrapListView(axis));
        foreach (var item in items) layout.Add(item);
        return layout.Apply(space);
    }

    private sealed class FixedView(float width, float height) : ContentView
    {
        protected override Vector2 LayoutContent(in Vector2 availableSpace)
        {
            return new Vector2(width, height);
        }

        public override void CollectContent(in Matrix4x4 transform, CommandList commands)
        {
        }
    }
}
