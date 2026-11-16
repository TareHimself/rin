using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;

namespace Rin.World.Tests.Physics;

/// <summary>
///     Exercises the real <see cref="BepuPhysicsSystem" />, guarding against the compounding-decay bug.
/// </summary>
public class TimeDilationTests : BepuFixture
{
    private static Transform StartTransform => new() { Position = new Vector3(0, 50, 0), Orientation = Quaternion.Identity, Scale = Vector3.One };

    [Test]
    public void FullyDilatedBodyDoesNotDriftWhileNormalBodyFalls()
    {
        var frozen = Bepu.CreateSphere(0.5f, StartTransform, PhysicsState.Simulated);
        // Offset so the spheres don't overlap - contact response would move "frozen" too (not a position lock).
        var normal = Bepu.CreateSphere(0.5f, StartTransform with { Position = StartTransform.Position + new Vector3(5, 0, 0) }, PhysicsState.Simulated);
        Bepu.SetTimeScale(frozen, 0f);

        for (var i = 0; i < 30; i++) Bepu.Update(1f / 60f);

        Assert.That(Bepu.GetPosition(frozen).Y, Is.EqualTo(StartTransform.Position.Y).Within(1e-3f),
            "a fully time-scaled-to-zero body should not move even though gravity keeps pulling on it internally");
        Assert.That(Bepu.GetPosition(normal).Y, Is.LessThan(StartTransform.Position.Y - 0.1f),
            "the undilated sibling should have visibly fallen over the same steps");
    }

    [Test]
    public void PartiallyDilatedBodyFallsProportionallySlowerWithoutDecayingAcrossManySteps()
    {
        const float scale = 0.25f;
        var slow = Bepu.CreateSphere(0.5f, StartTransform, PhysicsState.Simulated);
        var normal = Bepu.CreateSphere(0.5f, StartTransform with { Position = StartTransform.Position + new Vector3(5, 0, 0) }, PhysicsState.Simulated);
        Bepu.SetTimeScale(slow, scale);

        for (var i = 0; i < 40; i++) Bepu.Update(1f / 60f);

        var slowSpeed = Bepu.GetLinearVelocity(slow).Length();
        var normalSpeed = Bepu.GetLinearVelocity(normal).Length();

        Assert.That(normalSpeed, Is.GreaterThan(0.1f), "sanity check: the undilated body should actually be falling");
        var ratio = slowSpeed / normalSpeed;
        Assert.That(ratio, Is.EqualTo(scale).Within(0.05f),
            $"after many substeps the dilated body's speed should still track {scale}x the normal body's, not have decayed toward zero (actual ratio {ratio})");
    }

    [Test]
    public void RestoringScaleToOneResumesAccumulatedMotionRatherThanRestartingFromRest()
    {
        var body = Bepu.CreateSphere(0.5f, StartTransform, PhysicsState.Simulated);
        Bepu.SetTimeScale(body, 0f);

        for (var i = 0; i < 20; i++) Bepu.Update(1f / 60f);
        Assert.That(Bepu.GetLinearVelocity(body).Y, Is.EqualTo(0f).Within(1e-4f),
            "reported velocity should be pinned to zero while fully frozen");

        Bepu.SetTimeScale(body, 1f);
        Bepu.Update(1f / 60f);

        Assert.That(Bepu.GetLinearVelocity(body).Y, Is.LessThan(-9f * (1f / 60f) * 1.5f),
            "unfreezing should immediately reflect the velocity accumulated while frozen, not restart from rest");
    }

    [Test]
    public void ManyDilatedBodiesIntegratedAcrossWorkerThreadsAllTrackTheirScaleAndRepeatExactly()
    {
        var first = SimulateDilatedGrid();
        var second = SimulateDilatedGrid();

        Assert.That(second, Is.EqualTo(first), "the same dilated scene must step identically across runs");
    }

    private static float[] SimulateDilatedGrid()
    {
        var physics = new BepuPhysicsSystem();
        const int count = 2048;
        const float scale = 0.25f;
        var bodies = new PhysicsBodyHandle[count];
        for (var i = 0; i < count; i++)
        {
            var position = new Vector3(i % 64 * 3f, 50, i / 64 * 3f);
            bodies[i] = physics.CreateSphere(0.5f, StartTransform with { Position = position }, PhysicsState.Simulated);
            physics.SetTimeScale(bodies[i], scale);
        }

        var normal = physics.CreateSphere(0.5f, StartTransform with { Position = new Vector3(-10, 50, -10) }, PhysicsState.Simulated);
        for (var step = 0; step < 40; step++) physics.Update(1f / 60f);

        var normalSpeed = physics.GetLinearVelocity(normal).Length();
        var heights = new float[count];
        for (var i = 0; i < count; i++)
        {
            Assert.That(physics.GetLinearVelocity(bodies[i]).Length() / normalSpeed, Is.EqualTo(scale).Within(0.05f),
                $"body {i} lost track of its time scale");
            heights[i] = physics.GetPosition(bodies[i]).Y;
        }

        physics.Destroy();
        return heights;
    }
}
