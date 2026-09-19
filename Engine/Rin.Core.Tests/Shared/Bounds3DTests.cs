using System.Numerics;
using Rin.Core.Shared;

namespace Rin.Core.Tests.Shared;

public class Bounds3DTests
{
    [Test]
    public void AdditionOperatorUnionsTwoBounds()
    {
        var a = new Bounds3D { Min = new Vector3(0, 0, 0), Max = new Vector3(1, 1, 1) };
        var b = new Bounds3D { Min = new Vector3(-1, 2, 0), Max = new Vector3(0.5f, 3, 5) };

        var union = a + b;

        Assert.That(union.Min, Is.EqualTo(new Vector3(-1, 0, 0)));
        Assert.That(union.Max, Is.EqualTo(new Vector3(1, 3, 5)));
    }

    [Test]
    public void FromVectorProducesAZeroSizedBoundsAtThatPoint()
    {
        var bounds = Bounds3D.FromVector(new Vector3(2, 3, 4));

        Assert.That(bounds.Min, Is.EqualTo(new Vector3(2, 3, 4)));
        Assert.That(bounds.Max, Is.EqualTo(new Vector3(2, 3, 4)));
    }

    [Test]
    public void UpdateExpandsToIncludeANewPoint()
    {
        var bounds = Bounds3D.FromVector(Vector3.Zero);

        bounds.Update(new Vector3(-1, 5, 0.5f));

        Assert.That(bounds.Min, Is.EqualTo(new Vector3(-1, 0, 0)));
        Assert.That(bounds.Max, Is.EqualTo(new Vector3(0, 5, 0.5f)));
    }

    [Test]
    public void TransformByAxisAlignedScaleKeepsMinLessThanMax()
    {
        var bounds = new Bounds3D { Min = new Vector3(-1, -1, -1), Max = new Vector3(1, 1, 1) };

        var transformed = bounds.Transform(Matrix4x4.CreateScale(2f));

        Assert.That(transformed.Min, Is.EqualTo(new Vector3(-2, -2, -2)));
        Assert.That(transformed.Max, Is.EqualTo(new Vector3(2, 2, 2)));
    }

    [Test]
    public void TransformByNegativeScaleStillReportsAConsistentMinAndMax()
    {
        // A negative scale flips which corner is smaller, so Transform must re-sort Min/Max afterward.
        var bounds = new Bounds3D { Min = new Vector3(1, 1, 1), Max = new Vector3(2, 2, 2) };

        var transformed = bounds.Transform(Matrix4x4.CreateScale(-1f));

        Assert.That(transformed.Min.X, Is.LessThanOrEqualTo(transformed.Max.X));
        Assert.That(transformed.Min.Y, Is.LessThanOrEqualTo(transformed.Max.Y));
        Assert.That(transformed.Min.Z, Is.LessThanOrEqualTo(transformed.Max.Z));
        Assert.That(transformed.Min, Is.EqualTo(new Vector3(-2, -2, -2)));
        Assert.That(transformed.Max, Is.EqualTo(new Vector3(-1, -1, -1)));
    }
}
