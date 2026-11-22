using System.Numerics;
using Rin.World.Physics;

namespace Rin.World.Tests.Physics;

public class ColliderShapeTests
{
    [Test]
    public void ShapesOfTheSameKindAndSizeAreEqual()
    {
        var handle = new PhysicsShapeHandle(7);

        Assert.That(ColliderShape.CompoundOf(handle), Is.EqualTo(ColliderShape.CompoundOf(handle)));
        Assert.That(ColliderShape.Capsule(1f, 2f), Is.EqualTo(ColliderShape.Capsule(1f, 2f)));
        Assert.That(ColliderShape.Sphere(1f), Is.Not.EqualTo(ColliderShape.Capsule(1f, 0f)));
        Assert.That(ColliderShape.Box(Vector3.One), Is.Not.EqualTo(ColliderShape.Box(Vector3.UnitX)));
    }

    [Test]
    public void ACompoundCannotContainACompound()
    {
        var shapes = new PhysicsShapes();
        var inner = shapes.AddCompound([new ShapePart(ColliderShape.Sphere(1f), Vector3.Zero)]);

        Assert.Throws<ArgumentException>(() => shapes.AddCompound([new ShapePart(ColliderShape.CompoundOf(inner), Vector3.Zero)]));
    }
}
