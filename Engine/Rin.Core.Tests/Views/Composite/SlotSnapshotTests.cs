using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Graphics;

namespace Rin.Core.Tests.Views.Composite;

public class SlotSnapshotTests : FakeApplicationFixture
{
    [Test]
    public void GetSlotsReturnsTheSameArrayUntilASlotChanges()
    {
        var switcher = new SwitcherView();
        switcher.Add(new SpyView());

        Assert.That(switcher.GetSlots(), Is.SameAs(switcher.GetSlots()));
    }

    [Test]
    public void RemovingAChildDoesNotChangeASnapshotAlreadyTaken()
    {
        var switcher = new SwitcherView();
        var a = new SpyView();
        var b = new SpyView();
        switcher.Add(a);
        switcher.Add(b);
        var before = switcher.GetSlots();

        switcher.Remove(a);

        Assert.That(before.Select(s => s.Child), Is.EqualTo(new IView[] { a, b }));
        Assert.That(switcher.GetSlots().Select(s => s.Child), Is.EqualTo(new IView[] { b }));
    }

    [Test]
    public void AddingWhileIteratingASnapshotDoesNotThrowOrExtendTheLoop()
    {
        var switcher = new SwitcherView();
        switcher.Add(new SpyView());
        switcher.Add(new SpyView());

        var visited = 0;
        foreach (var _ in switcher.GetSlots())
        {
            switcher.Add(new SpyView());
            visited++;
        }

        Assert.That(visited, Is.EqualTo(2));
        Assert.That(switcher.GetSlots(), Has.Length.EqualTo(4));
    }

    [Test]
    public void GetActiveSlotsIsCachedAndFollowsSelectionAndRemoval()
    {
        var switcher = new SwitcherView();
        var a = new SpyView();
        var b = new SpyView();
        switcher.Add(a);
        switcher.Add(b);

        Assert.That(switcher.GetActiveSlots(), Is.SameAs(switcher.GetActiveSlots()));

        switcher.SelectedIndex = 1;
        Assert.That(switcher.GetActiveSlots().Select(s => s.Child), Is.EqualTo(new IView[] { b }));

        switcher.Remove(b);
        Assert.That(switcher.GetActiveSlots(), Is.Empty);
    }

    [Test]
    public void SingleSlotViewSlotsFollowSetChildAndAreCached()
    {
        var rect = new RectView();
        Assert.That(rect.GetSlots(), Is.Empty);

        var child = new SpyView();
        rect.SetChild(child);
        Assert.That(rect.GetSlots().Select(s => s.Child), Is.EqualTo(new IView[] { child }));
        Assert.That(rect.GetSlots(), Is.SameAs(rect.GetSlots()));

        rect.SetChild(null);
        Assert.That(rect.GetSlots(), Is.Empty);
    }

    [Test]
    public void CollapsedChildrenTakeNoSpaceInAList()
    {
        var list = new ListView(Rin.Core.Views.Layouts.Axis.Column);
        var shown = new SizedView();
        var hidden = new SizedView { Visibility = Visibility.Collapsed };
        list.Add(shown);
        list.Add(hidden);

        list.Layout(new Vector2(100f, 100f));

        Assert.That(hidden.GetSize(), Is.EqualTo(Vector2.Zero));
        Assert.That(list.GetContentSize().Y, Is.EqualTo(30f));
    }

    private sealed class SpyView : View
    {
        protected override Vector2 LayoutContent(in Vector2 availableSpace) => availableSpace;

        public override void Collect(in Matrix4x4 transform, in Rect2D clip, CommandList commands)
        {
        }
    }

    private sealed class SizedView : View
    {
        protected override Vector2 LayoutContent(in Vector2 availableSpace) => new(50f, 30f);

        public override void Collect(in Matrix4x4 transform, in Rect2D clip, CommandList commands)
        {
        }
    }
}
