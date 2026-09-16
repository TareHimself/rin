using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;

namespace Rin.World.Tests;

/// <summary>
///     Exercises the real <see cref="BepuPhysicsSystem" /> query API against the actual installed
///     Bepu package (2.5.0-beta.25), not the GitHub HEAD signatures.
/// </summary>
public class PhysicsQueryTests
{
    private static Transform At(Vector3 position) => new() { Position = position, Orientation = Quaternion.Identity, Scale = Vector3.One };

    [Test]
    public void RayCastHitsClosestSphereFirst()
    {
        var physics = new BepuPhysicsSystem();
        var near = physics.CreateSphere(1f, At(new Vector3(0, 0, 5)), PhysicsState.Static);
        var far = physics.CreateSphere(1f, At(new Vector3(0, 0, 10)), PhysicsState.Static);

        var result = physics.RayCast(Vector3.Zero, Vector3.UnitZ, 20f);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Body, Is.EqualTo(near));
        Assert.That(result.Value.Distance, Is.EqualTo(4f).Within(0.01f));
    }

    [Test]
    public void RayCastMisses()
    {
        var physics = new BepuPhysicsSystem();
        physics.CreateSphere(1f, At(new Vector3(10, 0, 0)), PhysicsState.Static);

        Assert.That(physics.RayCast(Vector3.Zero, Vector3.UnitZ, 20f), Is.Null);
    }

    [Test]
    public void RayCastAllReturnsEveryPiercedBody()
    {
        var physics = new BepuPhysicsSystem();
        var a = physics.CreateSphere(1f, At(new Vector3(0, 0, 5)), PhysicsState.Static);
        var b = physics.CreateSphere(1f, At(new Vector3(0, 0, 10)), PhysicsState.Static);

        var results = physics.RayCastAll(Vector3.Zero, Vector3.UnitZ, 20f);

        Assert.That(results.Select(r => r.Body), Is.EquivalentTo(new[] { a, b }));
    }

    [Test]
    public void ChannelFilterExcludesNonMatchingBodies()
    {
        var physics = new BepuPhysicsSystem();
        var enemy = physics.CreateSphere(1f, At(new Vector3(0, 0, 5)), PhysicsState.Static);
        physics.SetCollisionChannel(enemy, 1);
        var scenery = physics.CreateSphere(1f, At(new Vector3(0, 0, 5)), PhysicsState.Static);
        physics.SetCollisionChannel(scenery, 2);

        var result = physics.RayCast(Vector3.Zero, Vector3.UnitZ, 20f, channel: 1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Body, Is.EqualTo(enemy));
    }

    [Test]
    public void SphereCastHitsSomethingARayWouldMiss()
    {
        var physics = new BepuPhysicsSystem();
        // Offset to the side, just past a bare ray's path but within a wide swept sphere's radius.
        var target = physics.CreateSphere(0.5f, At(new Vector3(1.2f, 0, 5)), PhysicsState.Static);

        Assert.That(physics.RayCast(Vector3.Zero, Vector3.UnitZ, 20f), Is.Null, "sanity check: a bare ray should miss");

        var swept = physics.SphereCast(1f, Vector3.Zero, Vector3.UnitZ, 20f);
        Assert.That(swept, Is.Not.Null);
        Assert.That(swept!.Value.Body, Is.EqualTo(target));
    }

    [Test]
    public void OverlapSphereFindsBodyAtRestNoMotionNeeded()
    {
        var physics = new BepuPhysicsSystem();
        var body = physics.CreateSphere(1f, At(new Vector3(5, 0, 0)), PhysicsState.Static);
        physics.CreateSphere(1f, At(new Vector3(50, 0, 0)), PhysicsState.Static);

        var hits = physics.OverlapSphere(2f, new Vector3(5, 0, 0));

        Assert.That(hits, Is.EquivalentTo(new[] { body }));
    }

    [Test]
    public void OverlapCapsuleRespectsChannelFilter()
    {
        var physics = new BepuPhysicsSystem();
        var matching = physics.CreateSphere(0.5f, At(Vector3.Zero), PhysicsState.Static);
        physics.SetCollisionChannel(matching, 7);
        var other = physics.CreateSphere(0.5f, At(Vector3.Zero), PhysicsState.Static);
        physics.SetCollisionChannel(other, 9);

        var hits = physics.OverlapCapsule(1f, 1f, Vector3.Zero, Quaternion.Identity, channel: 7);

        Assert.That(hits, Is.EquivalentTo(new[] { matching }));
    }
}
