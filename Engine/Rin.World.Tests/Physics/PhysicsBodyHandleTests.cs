using Rin.World.Physics;

namespace Rin.World.Tests.Physics;

public class PhysicsBodyHandleTests
{
    [Test]
    public void DefaultPhysicsBodyHandleIsInvalid()
    {
        Assert.That(PhysicsBodyHandle.Invalid.IsValid, Is.False);
    }

    [Test]
    public void PhysicsBodyHandlesWithDifferentVersionsAreNotEqual()
    {
        var first = new PhysicsBodyHandle(3, 1);
        var second = new PhysicsBodyHandle(3, 2);

        Assert.That(first, Is.Not.EqualTo(second));
    }
}
