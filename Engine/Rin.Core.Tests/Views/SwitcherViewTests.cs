using System.Numerics;
using Rin.Core.Audio;
using Rin.Core.Graphics;
using Rin.Core.Shared;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;

namespace Rin.Core.Tests.Views;

public class SwitcherViewTests
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
    public void GetSlots_returns_all_children_GetActiveSlots_returns_the_selected_one()
    {
        var switcher = new SwitcherView();
        var a = new SpyView();
        var b = new SpyView();
        var c = new SpyView();
        switcher.Add(a);
        switcher.Add(b);
        switcher.Add(c);
        switcher.SelectedIndex = 1;

        Assert.That(switcher.GetSlots().Select(s => s.Child), Is.EquivalentTo(new IView[] { a, b, c }));
        Assert.That(switcher.GetActiveSlots().Select(s => s.Child), Is.EqualTo(new IView[] { b }));

        switcher.SelectedIndex = 2;
        Assert.That(switcher.GetActiveSlots().Single().Child, Is.SameAs(c));
    }

    [Test]
    public void Attaching_surfaces_every_child_not_just_the_selected_one()
    {
        var switcher = new SwitcherView();
        var visible = new SpyView();
        var hidden = new SpyView();
        switcher.Add(visible);
        switcher.Add(hidden);
        switcher.SelectedIndex = 0;

        switcher.SetSurface(new FakeSurface());

        Assert.That(visible.Surface, Is.Not.Null);
        Assert.That(hidden.Surface, Is.Not.Null, "non-active switcher child never received the surface");
        Assert.That(hidden.AddedToSurfaceCount, Is.EqualTo(1));
    }

    [Test]
    public void Disposing_disposes_every_child_not_just_the_selected_one()
    {
        var switcher = new SwitcherView();
        var visible = new SpyView();
        var hidden = new SpyView();
        switcher.Add(visible);
        switcher.Add(hidden);
        switcher.SelectedIndex = 0;

        switcher.Dispose();

        Assert.That(visible.Disposed, Is.True);
        Assert.That(hidden.Disposed, Is.True, "non-active switcher child leaked on dispose");
    }

    private sealed class SpyView : View
    {
        public bool Disposed { get; private set; }
        public int AddedToSurfaceCount { get; private set; }

        protected override Vector2 LayoutContent(in Vector2 availableSpace) => availableSpace;

        public override void Collect(in Matrix4x4 transform, in Rect2D clip, CommandList commands)
        {
        }

        protected override void OnAddedToSurface(ISurface surface) => AddedToSurfaceCount++;

        public override void Dispose()
        {
            Disposed = true;
            base.Dispose();
        }
    }
}
