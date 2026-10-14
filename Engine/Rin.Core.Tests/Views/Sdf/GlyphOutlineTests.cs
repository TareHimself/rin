using System.Numerics;
using Rin.Core.Views.Sdf;

namespace Rin.Core.Tests.Views.Sdf;

public class GlyphOutlineTests
{
    [Test]
    public void ClockwiseGlyfOutlineIsKeptAfterTheFlip()
    {
        var outline = new GlyphOutline();
        Square(outline, 0f, 0f, 10f, clockwise: true);

        var contours = outline.OrientedContours();

        Assert.That(contours, Has.Count.EqualTo(1));
        Assert.That(contours[0].SignedArea(), Is.GreaterThan(0f));
        Assert.That(contours[0].Start, Is.EqualTo(new Vector2(0f, 0f)));
    }

    [Test]
    public void CounterClockwiseOutlineIsReversed()
    {
        var outline = new GlyphOutline();
        Square(outline, 0f, 0f, 10f, clockwise: false);

        var contours = outline.OrientedContours();

        Assert.That(contours[0].SignedArea(), Is.GreaterThan(0f));
        Assert.That(contours[0].Segments, Has.Count.EqualTo(4));
        Assert.That(contours[0].Segments[^1].To, Is.EqualTo(contours[0].Start));
    }

    [Test]
    public void TheLargestContourDecidesAndHolesFollowIt()
    {
        var outline = new GlyphOutline();
        Square(outline, 0f, 0f, 10f, clockwise: false);
        Square(outline, 3f, 3f, 4f, clockwise: true);

        var contours = outline.OrientedContours();

        Assert.That(contours[0].SignedArea(), Is.GreaterThan(0f));
        Assert.That(contours[1].SignedArea(), Is.LessThan(0f));
    }

    [Test]
    public void ScaleAppliesToEveryPoint()
    {
        var outline = new GlyphOutline(0.5f);
        Square(outline, 0f, 0f, 10f, clockwise: true);

        var contour = outline.OrientedContours()[0];

        Assert.That(contour.Segments[1].To, Is.EqualTo(new Vector2(5f, -5f)));
    }

    [Test]
    public void ReversedCubicSwapsItsControlPoints()
    {
        var contour = new Contour(new Vector2(0f, 0f));
        contour.Segments.Add(new Segment(SegmentKind.Cubic, new Vector2(1f, 2f), new Vector2(3f, 4f), new Vector2(5f, 0f)));
        contour.Segments.Add(new Segment(SegmentKind.Line, default, default, new Vector2(0f, 0f)));

        var reversed = contour.Reversed();

        Assert.That(reversed.Start, Is.EqualTo(new Vector2(0f, 0f)));
        Assert.That(reversed.Segments[1].Kind, Is.EqualTo(SegmentKind.Cubic));
        Assert.That(reversed.Segments[1].Control1, Is.EqualTo(new Vector2(3f, 4f)));
        Assert.That(reversed.Segments[1].Control2, Is.EqualTo(new Vector2(1f, 2f)));
        Assert.That(reversed.Segments[1].To, Is.EqualTo(new Vector2(0f, 0f)));
        Assert.That(reversed.SignedArea(), Is.EqualTo(-contour.SignedArea()).Within(3).Percent);
    }

    [Test]
    public void CurvedContourAreaHasTheRightSign()
    {
        var outline = new GlyphOutline();
        outline.MoveTo(0f, 0f);
        outline.QuadraticTo(5f, 10f, 10f, 0f);
        outline.ClosePath();

        var area = outline.OrientedContours()[0].SignedArea();

        Assert.That(area, Is.GreaterThan(0f));
    }

    /// <summary>
    ///     A square in font space, where Y points up, so the clockwise one is what a glyf font stores.
    /// </summary>
    private static void Square(GlyphOutline outline, float x, float y, float size, bool clockwise)
    {
        outline.MoveTo(x, y);
        if (clockwise)
        {
            outline.LineTo(x, y + size);
            outline.LineTo(x + size, y + size);
            outline.LineTo(x + size, y);
        }
        else
        {
            outline.LineTo(x + size, y);
            outline.LineTo(x + size, y + size);
            outline.LineTo(x, y + size);
        }

        outline.LineTo(x, y);
        outline.ClosePath();
    }
}
