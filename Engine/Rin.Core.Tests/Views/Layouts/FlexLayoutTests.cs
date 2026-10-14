using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Layouts;

namespace Rin.Core.Tests.Views.Layouts;

public class FlexLayoutTests
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
    public void Column_with_finite_width_fills_it_even_when_its_children_are_narrower()
    {
        var column = new FlexBoxView(Axis.Column);
        var child = new FixedView(new Vector2(50f, 20f));
        column.Add(child);

        var size = column.Layout(new Vector2(300f, float.PositiveInfinity));

        Assert.That(size.X, Is.EqualTo(300f));
        Assert.That(child.GetSize().X, Is.EqualTo(50f));
    }

    [Test]
    public void Column_child_with_fill_takes_the_available_width()
    {
        var column = new FlexBoxView(Axis.Column);
        var child = new FixedView(new Vector2(50f, 20f));
        column.Add(new FlexBoxSlot { Child = child, Fit = CrossFit.Fill });

        column.Layout(new Vector2(300f, float.PositiveInfinity));

        Assert.That(child.GetSize().X, Is.EqualTo(300f));
    }

    [Test]
    public void Column_with_infinite_width_sizes_to_the_widest_child()
    {
        var column = new FlexBoxView(Axis.Column);
        column.Add(new FixedView(new Vector2(50f, 20f)));
        column.Add(new FixedView(new Vector2(80f, 20f)));

        var size = column.Layout(new Vector2(float.PositiveInfinity, float.PositiveInfinity));

        Assert.That(size, Is.EqualTo(new Vector2(80f, 40f)));
    }

    [Test]
    public void Column_centers_a_child_within_the_available_width()
    {
        var column = new FlexBoxView(Axis.Column);
        var child = new FixedView(new Vector2(50f, 20f));
        column.Add(new FlexBoxSlot { Child = child, Align = CrossAlign.Center });

        column.Layout(new Vector2(300f, float.PositiveInfinity));

        Assert.That(child.Offset.X, Is.EqualTo(125f));
    }

    [Test]
    public void Row_with_finite_height_fills_it()
    {
        var row = new FlexBoxView(Axis.Row);
        var child = new FixedView(new Vector2(50f, 20f));
        row.Add(new FlexBoxSlot { Child = child, Fit = CrossFit.Fill });

        var size = row.Layout(new Vector2(float.PositiveInfinity, 100f));

        Assert.That(size.Y, Is.EqualTo(100f));
        Assert.That(child.GetSize().Y, Is.EqualTo(100f));
    }

    [Test]
    public void Flex_slots_share_the_main_axis_left_by_fixed_children()
    {
        var row = new FlexBoxView(Axis.Row);
        var fixedChild = new FixedView(new Vector2(100f, 20f));
        var a = new FixedView(new Vector2(0f, 20f));
        var b = new FixedView(new Vector2(0f, 20f));
        row.Add(fixedChild);
        row.Add(new FlexBoxSlot { Child = a, Flex = 1f });
        row.Add(new FlexBoxSlot { Child = b, Flex = 3f });

        row.Layout(new Vector2(500f, float.PositiveInfinity));

        Assert.That(a.GetSize().X, Is.EqualTo(100f));
        Assert.That(b.GetSize().X, Is.EqualTo(300f));
        Assert.That(b.Offset.X, Is.EqualTo(200f));
    }

    private sealed class FixedView(Vector2 desired) : View
    {
        public override Vector2 ComputeDesiredContentSize()
        {
            return desired;
        }

        protected override Vector2 LayoutContent(in Vector2 availableSpace)
        {
            return new Vector2(
                float.IsFinite(availableSpace.X) ? availableSpace.X : desired.X,
                float.IsFinite(availableSpace.Y) ? availableSpace.Y : desired.Y);
        }

        public override void Collect(in Matrix4x4 transform, in Rect2D clip, CommandList commands)
        {
        }
    }
}
