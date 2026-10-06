using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Shared.Math;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Layouts;

namespace Rin.Core.Tests.Views.Composite;

public class VirtualListViewTests
{
    private const float ItemSize = 20f;

    private readonly List<int> _bindCalls = [];

    [SetUp]
    public void SetUp()
    {
        IApplication.Override = new FakeApplication();
        _bindCalls.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        IApplication.Override = null;
    }

    [Test]
    public void OnlyCreatesViewsForTheVisibleWindow()
    {
        var list = MakeList(100_000);

        list.Layout(new Vector2(200f, 100f));
        list.Update(0f);

        Assert.That(list.RowCount, Is.LessThan(20));
        Assert.That(list.GetSlots(), Has.Length.EqualTo(list.RowCount));
    }

    [Test]
    public void MaxScrollCoversAllItems()
    {
        var list = MakeList(1000);

        list.Layout(new Vector2(200f, 100f));

        Assert.That(list.GetMaxScroll(), Is.EqualTo(1000 * ItemSize - 100f));
    }

    [Test]
    public void BindsEachVisibleIndexOnceAndRowsSitAtTheirIndex()
    {
        var list = MakeList(1000);
        list.Layout(new Vector2(200f, 100f));
        list.Update(0f);

        var bound = BoundRows(list);

        Assert.That(bound.Select(r => r.Index), Is.Unique);
        Assert.That(bound.Select(r => r.Index), Does.Contain(0).And.Contain(4));
        foreach (var (row, index) in bound) Assert.That(row.Offset.Y, Is.EqualTo(index * ItemSize));
    }

    [Test]
    public void ScrollingRebindsOnlyTheRowsThatEnteredTheWindow()
    {
        var list = MakeList(1000);
        list.Layout(new Vector2(200f, 100f));
        list.Update(0f);
        _bindCalls.Clear();

        list.ScrollBy(ItemSize);
        list.Update(0f);

        Assert.That(_bindCalls, Has.Count.EqualTo(1));
        Assert.That(BoundRows(list).Select(r => r.Index), Does.Contain(5 + 2));
    }

    [Test]
    public void CollectBindsRowsForScrollAndDataChangesMadeSinceTheLastUpdate()
    {
        var list = MakeList(1000);
        list.Layout(new Vector2(200f, 100f));
        list.Update(0f);

        list.ScrollToEnd();
        list.Collect(Matrix4x4.Identity, new Rect2D(new Vector2(-10000f), new Vector2(20000f)),
            new CommandList { SurfaceSize = new Vector2(800f, 600f) });

        Assert.That(BoundRows(list).Select(r => r.Index), Does.Contain(999).And.Contain(995));
    }

    [Test]
    public void ScrollingToTheEndShowsTheLastItem()
    {
        var list = MakeList(1000);
        list.Layout(new Vector2(200f, 100f));

        list.ScrollToEnd();
        list.Update(0f);

        Assert.That(list.IsAtEnd, Is.True);
        Assert.That(BoundRows(list).Select(r => r.Index), Does.Contain(999));
    }

    [Test]
    public void ShrinkingTheItemCountUnbindsRowsPastTheEnd()
    {
        var list = MakeList(1000);
        list.Layout(new Vector2(200f, 100f));
        list.Update(0f);

        list.ItemCount = 3;
        list.Update(0f);

        Assert.That(BoundRows(list).Select(r => r.Index), Is.EquivalentTo(new[] { 0, 1, 2 }));
        Assert.That(list.GetMaxScroll(), Is.EqualTo(0f));
    }

    [Test]
    public void RefreshRebindsEveryVisibleRow()
    {
        var list = MakeList(1000);
        list.Layout(new Vector2(200f, 100f));
        list.Update(0f);
        var visible = BoundRows(list).Count;
        _bindCalls.Clear();

        list.Refresh();
        list.Update(0f);

        Assert.That(_bindCalls, Has.Count.EqualTo(visible));
    }

    [Test]
    public void DraggingTheBarScrollsProportionally()
    {
        var root = new RootView();
        var list = MakeList(1000);
        root.Add(list);
        root.Layout(new Vector2(200f, 100f), true);
        var surface = new FakeSurface();
        var contentWidth = list.GetContentSize().X;
        var barPosition = new Vector2(contentWidth - list.BarPadding - list.BarWidth / 2f, 5f);

        var down = new CursorDownSurfaceEvent(surface, CursorButton.One, barPosition);
        root.HandleEvent(down, root.GetLocalTransform());
        var move = new CursorMoveSurfaceEvent(surface, barPosition + new Vector2(0f, 20f));
        root.HandleEvent(move, root.GetLocalTransform());

        var travel = 100f - list.BarMinimumSize;
        Assert.That(list.GetScroll(), Is.EqualTo(20f / travel * list.GetMaxScroll()).Within(1f));
    }

    private VirtualListView MakeList(int itemCount)
    {
        return new VirtualListView(() => new RowView(), Bind, ItemSize) { ItemCount = itemCount };
    }

    private void Bind(IView view, int index)
    {
        _bindCalls.Add(index);
        ((RowView)view).Index = index;
    }

    private static List<(RowView Row, int Index)> BoundRows(VirtualListView list)
    {
        return list.GetSlots().Select(s => (RowView)s.Child).Where(r => r.IsVisible).Select(r => (r, r.Index)).ToList();
    }

    private sealed class RowView : ContentView
    {
        public int Index { get; set; } = -1;

        protected override Vector2 LayoutContent(in Vector2 availableSpace) => availableSpace;

        public override void CollectContent(in Matrix4x4 transform, CommandList commands)
        {
        }
    }
}
