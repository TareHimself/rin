using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;

namespace Rin.World.Tests;

/// <summary>
///     Exercises the real <see cref="BepuPhysicsSystem" />, guarding against the compounding-decay bug.
/// </summary>
public class TimeDilationTests
{
    private static Transform StartTransform => new() { Position = new Vector3(0, 50, 0), Orientation = Quaternion.Identity, Scale = Vector3.One };

    [Test]
    public void FullyDilatedBodyDoesNotDriftWhileNormalBodyFalls()
    {
        var physics = new BepuPhysicsSystem();
        var frozen = physics.CreateSphere(0.5f, StartTransform, PhysicsState.Simulated);
        var normal = physics.CreateSphere(0.5f, StartTransform, PhysicsState.Simulated);
        physics.SetTimeScale(frozen, 0f);

        for (var i = 0; i < 30; i++) physics.Update(1f / 60f);

        Assert.That(physics.GetPosition(frozen).Y, Is.EqualTo(StartTransform.Position.Y).Within(1e-3f),
            "a fully time-scaled-to-zero body should not move even though gravity keeps pulling on it internally");
        Assert.That(physics.GetPosition(normal).Y, Is.LessThan(StartTransform.Position.Y - 0.1f),
            "the undilated sibling should have visibly fallen over the same steps");
    }

    [Test]
    public void PartiallyDilatedBodyFallsProportionallySlowerWithoutDecayingAcrossManySteps()
    {
        var physics = new BepuPhysicsSystem();
        const float scale = 0.25f;
        var slow = physics.CreateSphere(0.5f, StartTransform, PhysicsState.Simulated);
        var normal = physics.CreateSphere(0.5f, StartTransform, PhysicsState.Simulated);
        physics.SetTimeScale(slow, scale);

        for (var i = 0; i < 40; i++) physics.Update(1f / 60f);

        var slowSpeed = physics.GetLinearVelocity(slow).Length();
        var normalSpeed = physics.GetLinearVelocity(normal).Length();

        Assert.That(normalSpeed, Is.GreaterThan(0.1f), "sanity check: the undilated body should actually be falling");
        var ratio = slowSpeed / normalSpeed;
        Assert.That(ratio, Is.EqualTo(scale).Within(0.05f),
            $"after many substeps the dilated body's speed should still track {scale}x the normal body's, not have decayed toward zero (actual ratio {ratio})");
    }

    [Test]
    public void RestoringScaleToOneResumesAccumulatedMotionRatherThanRestartingFromRest()
    {
        var physics = new BepuPhysicsSystem();
        var body = physics.CreateSphere(0.5f, StartTransform, PhysicsState.Simulated);
        physics.SetTimeScale(body, 0f);

        for (var i = 0; i < 20; i++) physics.Update(1f / 60f);
        Assert.That(physics.GetLinearVelocity(body).Y, Is.EqualTo(0f).Within(1e-4f),
            "reported velocity should be pinned to zero while fully frozen");

        physics.SetTimeScale(body, 1f);
        physics.Update(1f / 60f);

        Assert.That(physics.GetLinearVelocity(body).Y, Is.LessThan(-9f * (1f / 60f) * 1.5f),
            "unfreezing should immediately reflect the velocity accumulated while frozen, not restart from rest");
    }
}
