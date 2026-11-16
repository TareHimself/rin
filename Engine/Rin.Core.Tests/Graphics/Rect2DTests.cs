using System.Numerics;
using Rin.Core.Graphics;

namespace Rin.Core.Tests.Graphics;

public class Rect2DTests
{
    private static readonly Rect2D Area = new(new Vector2(10f, 10f), new Vector2(10f, 10f));

    private static Rect2D Square(float x, float y, float size = 4f) => new(new Vector2(x, y), new Vector2(size));

    private static Matrix4x4 QuarterTurnAt(float x, float y) =>
        Matrix4x4.CreateRotationZ(MathF.PI / 2f) * Matrix4x4.CreateTranslation(x, y, 0f);

    [Test]
    public void ATransformedRectIsTheBoundingBoxOfItsCorners()
    {
        var rect = new Rect2D(Vector2.Zero, new Vector2(4f, 2f), QuarterTurnAt(10f, 0f));

        Assert.That(Vector2.Distance(rect.Offset, new Vector2(8f, 0f)), Is.LessThan(1e-4f));
        Assert.That(Vector2.Distance(rect.Size, new Vector2(2f, 4f)), Is.LessThan(1e-4f));
    }

    [TestCase(8f, 8f)]
    [TestCase(18f, 8f)]
    [TestCase(8f, 18f)]
    [TestCase(18f, 18f)]
    [TestCase(13f, 13f)]
    [TestCase(6f, 6f)]
    [TestCase(20f, 20f)]
    public void RectsThatOverlapOrTouchIntersectFromEitherSide(float x, float y)
    {
        var other = Square(x, y);

        Assert.That(Rect2D.IntersectsWith(Area, other), Is.True);
        Assert.That(Rect2D.IntersectsWith(other, Area), Is.True);
    }

    [TestCase(0f, 12f)]
    [TestCase(21f, 12f)]
    [TestCase(12f, 0f)]
    [TestCase(12f, 21f)]
    [TestCase(21f, 21f)]
    public void RectsApartOnEitherAxisDoNotIntersect(float x, float y)
    {
        var other = Square(x, y);

        Assert.That(Rect2D.IntersectsWith(Area, other), Is.False);
        Assert.That(Rect2D.IntersectsWith(other, Area), Is.False);
    }

    [Test]
    public void ClampKeepsOnlyThePartInsideTheArea()
    {
        Assert.That(Rect2D.Clamp(Square(8f, 18f), Area), Is.EqualTo(new Rect2D(new Vector2(10f, 18f), new Vector2(2f, 2f))));
    }

    [Test]
    public void ClampLeavesARectInsideTheAreaUnchanged()
    {
        Assert.That(Rect2D.Clamp(Square(12f, 12f), Area), Is.EqualTo(Square(12f, 12f)));
    }

    [Test]
    public void ClampOfARectOutsideTheAreaIsEmpty()
    {
        Assert.That(Rect2D.Clamp(Square(40f, 40f), Area), Is.EqualTo(new Rect2D(Area.Offset, Vector2.Zero)));
    }

    [TestCase(9f, 2f, true)]
    [TestCase(8.5f, 3.5f, true)]
    [TestCase(11f, 2f, false)]
    [TestCase(9f, 5f, false)]
    [TestCase(7f, 2f, false)]
    public void PointWithinFollowsTheTransformedRectBothWays(float x, float y, bool expected)
    {
        var size = new Vector2(4f, 2f);
        var transform = QuarterTurnAt(10f, 0f);
        var point = new Vector2(x, y);

        Assert.That(Rect2D.PointWithin(size, transform, point), Is.EqualTo(expected), "through the inverse");
        Assert.That(Rect2D.PointWithin(size, transform, point, false), Is.EqualTo(expected), "through the corners");
    }

    [TestCase(0f, 2.8f, true)]
    [TestCase(0f, 5f, true)]
    [TestCase(2f, 1f, false)]
    [TestCase(-2f, 1f, false)]
    [TestCase(2f, 5f, false)]
    [TestCase(-2f, 5f, false)]
    public void PointWithinARectTurnedOffAxisExcludesTheCornersOfItsBoundingBox(float x, float y, bool expected)
    {
        var size = new Vector2(4f, 4f);
        var transform = Matrix4x4.CreateRotationZ(MathF.PI / 4f);
        var point = new Vector2(x, y);

        Assert.That(Rect2D.PointWithin(size, transform, point), Is.EqualTo(expected), "through the inverse");
        Assert.That(Rect2D.PointWithin(size, transform, point, false), Is.EqualTo(expected), "through the corners");
    }
}
