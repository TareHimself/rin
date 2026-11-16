using System.Numerics;
using Rin.World.Physics;

namespace Rin.World.Tests.Physics;

public class BepuBodyMutationTests : BepuFixture
{
    private const float Step = 1f / 60f;

    private void StepFor(int steps)
    {
        for (var i = 0; i < steps; i++) Bepu.Update(Step);
    }

    private float DistanceAlongZTo(PhysicsBodyHandle body)
    {
        var hit = Bepu.RayCast(Vector3.Zero, Vector3.UnitZ, 50f);
        Assert.That(hit?.Body, Is.EqualTo(body));
        return hit!.Value.Distance;
    }

    [Test]
    public void ASimulatedBodyMadeStaticStopsFalling()
    {
        var ball = Bepu.CreateSphere(0.5f, At(new Vector3(0, 10, 0)), PhysicsState.Simulated);
        StepFor(10);
        var heightWhenFrozen = Bepu.GetPosition(ball).Y;

        Bepu.SetState(ball, PhysicsState.Static);
        StepFor(30);

        Assert.That(heightWhenFrozen, Is.LessThan(10f));
        Assert.That(Bepu.GetState(ball), Is.EqualTo(PhysicsState.Static));
        Assert.That(Bepu.GetPosition(ball).Y, Is.EqualTo(heightWhenFrozen).Within(1e-4f));
    }

    [Test]
    public void AStaticBodyMadeSimulatedStartsFalling()
    {
        var ball = Bepu.CreateSphere(0.5f, At(new Vector3(0, 10, 0)), PhysicsState.Static);

        Bepu.SetState(ball, PhysicsState.Simulated);
        StepFor(30);

        Assert.That(Bepu.GetPosition(ball).Y, Is.LessThan(9.9f));
    }

    [Test]
    public void GravityIsReadBackAndDrivesSimulatedBodies()
    {
        var ball = Bepu.CreateSphere(0.5f, At(Vector3.Zero), PhysicsState.Simulated);

        Bepu.SetGravity(new Vector3(3f, 0f, 0f));
        StepFor(30);

        var position = Bepu.GetPosition(ball);
        Assert.That(Bepu.GetGravity(), Is.EqualTo(new Vector3(3f, 0f, 0f)));
        Assert.That(position.X, Is.GreaterThan(0.1f));
        Assert.That(position.Y, Is.EqualTo(0f).Within(1e-4f));
    }

    [Test]
    public void MassIsReadBack()
    {
        var ball = Bepu.CreateSphere(0.5f, At(Vector3.Zero), PhysicsState.Simulated);

        Bepu.SetMass(ball, 4f);

        Assert.That(Bepu.GetMass(ball), Is.EqualTo(4f));
    }

    [Test]
    public void ResizingASphereMovesItsSurface()
    {
        var ball = Bepu.CreateSphere(1f, At(new Vector3(0, 0, 10)), PhysicsState.Static);

        Bepu.SetSphereRadius(ball, 3f);

        Assert.That(Bepu.GetSphereRadius(ball), Is.EqualTo(3f));
        Assert.That(DistanceAlongZTo(ball), Is.EqualTo(7f).Within(0.01f));
    }

    [Test]
    public void ResizingABoxMovesItsFaces()
    {
        var box = Bepu.CreateBox(new Vector3(2f), At(new Vector3(0, 0, 10)), PhysicsState.Static);

        Bepu.SetBoxSize(box, new Vector3(2f, 2f, 6f));

        Assert.That(Bepu.GetBoxSize(box), Is.EqualTo(new Vector3(2f, 2f, 6f)));
        Assert.That(DistanceAlongZTo(box), Is.EqualTo(7f).Within(0.01f));
    }

    [Test]
    public void ResizingACapsuleMovesItsSurface()
    {
        var capsule = Bepu.CreateCapsule(0.5f, 1f, At(new Vector3(0, 0, 10)), PhysicsState.Static);

        Bepu.SetCapsuleRadius(capsule, 2f);
        Bepu.SetCapsuleHalfHeight(capsule, 3f);

        Assert.That(Bepu.GetCapsuleRadius(capsule), Is.EqualTo(2f));
        Assert.That(Bepu.GetCapsuleHalfHeight(capsule), Is.EqualTo(3f));
        Assert.That(DistanceAlongZTo(capsule), Is.EqualTo(8f).Within(0.01f));
    }

    [Test]
    public void ABoxCastHitsWhatItsWidthReaches()
    {
        var target = Bepu.CreateSphere(0.5f, At(new Vector3(1.2f, 0, 5)), PhysicsState.Static);

        var narrow = Bepu.BoxCast(new Vector3(0.5f), Vector3.Zero, Quaternion.Identity, Vector3.UnitZ, 20f);
        var wide = Bepu.BoxCast(new Vector3(2f), Vector3.Zero, Quaternion.Identity, Vector3.UnitZ, 20f);

        Assert.That(narrow, Is.Null);
        Assert.That(wide?.Body, Is.EqualTo(target));
    }

    [Test]
    public void ACapsuleCastHitsWhatItsRadiusReaches()
    {
        var target = Bepu.CreateSphere(0.5f, At(new Vector3(1.2f, 0, 5)), PhysicsState.Static);

        var narrow = Bepu.CapsuleCast(0.2f, 0.5f, Vector3.Zero, Quaternion.Identity, Vector3.UnitZ, 20f);
        var wide = Bepu.CapsuleCast(1f, 0.5f, Vector3.Zero, Quaternion.Identity, Vector3.UnitZ, 20f);

        Assert.That(narrow, Is.Null);
        Assert.That(wide?.Body, Is.EqualTo(target));
    }

    [Test]
    public void BoxAndCapsuleCastAllVisitEveryBodyInThePath()
    {
        Bepu.CreateSphere(1f, At(new Vector3(0, 0, 5)), PhysicsState.Static);
        Bepu.CreateSphere(1f, At(new Vector3(0, 0, 12)), PhysicsState.Static);
        var boxHits = new CountingHandler();
        var capsuleHits = new CountingHandler();

        Bepu.BoxCastAll(new Vector3(1f), Vector3.Zero, Quaternion.Identity, Vector3.UnitZ, 20f, ref boxHits);
        Bepu.CapsuleCastAll(0.5f, 0.5f, Vector3.Zero, Quaternion.Identity, Vector3.UnitZ, 20f, ref capsuleHits);

        Assert.That(boxHits.Count, Is.EqualTo(2));
        Assert.That(capsuleHits.Count, Is.EqualTo(2));
    }

    private struct CountingHandler : IPhysicsHitHandler
    {
        public int Count;

        public bool OnHit(in RayCastResult hit)
        {
            Count++;
            return true;
        }
    }
}
