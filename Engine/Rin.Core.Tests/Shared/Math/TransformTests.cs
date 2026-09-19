using System.Numerics;
using Rin.Core.Shared.Math;

namespace Rin.Core.Tests.Shared.Math;

public class TransformTests
{
    [Test]
    public void DefaultTransformIsIdentity()
    {
        var transform = new Transform();

        Assert.That(transform.Position, Is.EqualTo(Vector3.Zero));
        Assert.That(transform.Orientation, Is.EqualTo(Quaternion.Identity));
        Assert.That(transform.Scale, Is.EqualTo(Vector3.One));
    }

    [Test]
    public void ToMatrixThenFromRoundTripsPositionOrientationAndScale()
    {
        var original = new Transform
        {
            Position = new Vector3(1, 2, 3),
            Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 3f),
            Scale = new Vector3(2, 1, 0.5f)
        };

        var roundTripped = Transform.From(original.ToMatrix());

        Assert.That(Vector3.Distance(roundTripped.Position, original.Position), Is.LessThan(1e-4f));
        Assert.That(Vector3.Distance(roundTripped.Scale, original.Scale), Is.LessThan(1e-4f));
        Assert.That(Quaternion.Dot(roundTripped.Orientation, original.Orientation), Is.EqualTo(1f).Within(1e-4f));
    }

    [Test]
    public void InParentSpaceComposesTranslationsAdditively()
    {
        var child = new Transform { Position = new Vector3(1, 0, 0) };
        var parent = new Transform { Position = new Vector3(5, 0, 0) };

        var world = child.InParentSpace(parent);

        Assert.That(Vector3.Distance(world.Position, new Vector3(6, 0, 0)), Is.LessThan(1e-4f));
    }

    [Test]
    public void DeconstructReturnsPositionOrientationAndScale()
    {
        var transform = new Transform
        {
            Position = new Vector3(1, 2, 3),
            Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.5f),
            Scale = new Vector3(1, 2, 3)
        };

        var (position, orientation, scale) = transform;

        Assert.That(position, Is.EqualTo(transform.Position));
        Assert.That(orientation, Is.EqualTo(transform.Orientation));
        Assert.That(scale, Is.EqualTo(transform.Scale));
    }
}
