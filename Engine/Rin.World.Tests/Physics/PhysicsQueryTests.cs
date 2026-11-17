using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;

namespace Rin.World.Tests.Physics;

/// <summary>
///     Exercises the real <see cref="BepuPhysicsSystem" /> query API against the actual installed
///     Bepu package (2.5.0-beta.25), not the GitHub HEAD signatures.
/// </summary>
public class PhysicsQueryTests : BepuFixture
{
    [Test]
    public void RayCastHitsClosestSphereFirst()
    {
        var near = Bepu.CreateSphere(1f, At(new Vector3(0, 0, 5)), PhysicsState.Static);
        var far = Bepu.CreateSphere(1f, At(new Vector3(0, 0, 10)), PhysicsState.Static);

        var result = Bepu.RayCast(Vector3.Zero, Vector3.UnitZ, 20f);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Body, Is.EqualTo(near));
        Assert.That(result.Value.Distance, Is.EqualTo(4f).Within(0.01f));
    }

    [Test]
    public void RayCastMisses()
    {
        Bepu.CreateSphere(1f, At(new Vector3(10, 0, 0)), PhysicsState.Static);

        Assert.That(Bepu.RayCast(Vector3.Zero, Vector3.UnitZ, 20f), Is.Null);
    }

    [Test]
    public void RayCastAllReturnsEveryPiercedBody()
    {
        var a = Bepu.CreateSphere(1f, At(new Vector3(0, 0, 5)), PhysicsState.Static);
        var b = Bepu.CreateSphere(1f, At(new Vector3(0, 0, 10)), PhysicsState.Static);

        var hits = new RayCastResult[8];
        var count = Bepu.RayCastAll(Vector3.Zero, Vector3.UnitZ, 20f, hits);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(hits.Take(count).Select(r => r.Body), Is.EquivalentTo(new[] { a, b }));
    }

    [Test]
    public void RayCastAllStopsWhenTheBufferIsFull()
    {
        Bepu.CreateSphere(1f, At(new Vector3(0, 0, 5)), PhysicsState.Static);
        Bepu.CreateSphere(1f, At(new Vector3(0, 0, 10)), PhysicsState.Static);
        Bepu.CreateSphere(1f, At(new Vector3(0, 0, 15)), PhysicsState.Static);

        var hits = new RayCastResult[2];
        var count = Bepu.RayCastAll(Vector3.Zero, Vector3.UnitZ, 20f, hits);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(Bepu.RayCastAll(Vector3.Zero, Vector3.UnitZ, 20f, Span<RayCastResult>.Empty), Is.Zero);
    }

    [Test]
    public void RayCastAllVisitorSeesEveryHitAndCanStopEarly()
    {
        Bepu.CreateSphere(1f, At(new Vector3(0, 0, 5)), PhysicsState.Static);
        Bepu.CreateSphere(1f, At(new Vector3(0, 0, 10)), PhysicsState.Static);
        Bepu.CreateSphere(1f, At(new Vector3(0, 0, 15)), PhysicsState.Static);

        var all = new CountingHandler { StopAfter = int.MaxValue };
        Bepu.RayCastAll(Vector3.Zero, Vector3.UnitZ, 20f, ref all);
        var two = new CountingHandler { StopAfter = 2 };
        Bepu.RayCastAll(Vector3.Zero, Vector3.UnitZ, 20f, ref two);

        Assert.That(all.Count, Is.EqualTo(3));
        Assert.That(two.Count, Is.EqualTo(2));
    }

    [Test]
    public void SphereCastAllFillsTheBufferAndSortByDistanceOrdersIt()
    {
        var near = Bepu.CreateSphere(1f, At(new Vector3(0, 0, 5)), PhysicsState.Static);
        var far = Bepu.CreateSphere(1f, At(new Vector3(0, 0, 12)), PhysicsState.Static);

        Span<RayCastResult> hits = stackalloc RayCastResult[4];
        var count = Bepu.SphereCastAll(0.5f, Vector3.Zero, Vector3.UnitZ, 20f, hits);
        hits = hits[..count];
        hits.SortByDistance();

        Assert.That(count, Is.EqualTo(2));
        Assert.That(hits[0].Body, Is.EqualTo(near));
        Assert.That(hits[1].Body, Is.EqualTo(far));
    }

    [Test]
    public void OverlapReportsAMeshOnceEvenWhenSeveralTrianglesOverlap()
    {
        Vector3[] vertices = [new(-5, 0, -5), new(5, 0, -5), new(5, 0, 5), new(-5, 0, 5)];
        uint[] indices = [0, 1, 2, 0, 2, 3];
        var floor = Bepu.CreateStaticMesh(vertices, indices, At(Vector3.Zero));

        var bodies = new PhysicsBodyHandle[8];
        var count = Bepu.OverlapSphere(3f, new Vector3(0, 0.5f, 0), bodies);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(bodies[0], Is.EqualTo(floor));
    }

    [Test]
    public void OverlapVisitorCanStopEarlyAndTheBufferFormTruncates()
    {
        for (var i = 0; i < 3; i++) Bepu.CreateSphere(0.5f, At(new Vector3(i, 0, 0)), PhysicsState.Static);

        var one = new CountingOverlapHandler { StopAfter = 1 };
        Bepu.OverlapSphere(5f, Vector3.Zero, ref one);
        var all = new CountingOverlapHandler { StopAfter = int.MaxValue };
        Bepu.OverlapSphere(5f, Vector3.Zero, ref all);
        var two = new PhysicsBodyHandle[2];

        Assert.That(one.Count, Is.EqualTo(1));
        Assert.That(all.Count, Is.EqualTo(3));
        Assert.That(Bepu.OverlapSphere(5f, Vector3.Zero, two), Is.EqualTo(2));
    }

    private struct CountingOverlapHandler : IPhysicsOverlapHandler
    {
        public int StopAfter;
        public int Count;

        public bool OnOverlap(PhysicsBodyHandle body)
        {
            Count++;
            return Count < StopAfter;
        }
    }

    private struct CountingHandler : IPhysicsHitHandler
    {
        public int StopAfter;
        public int Count;

        public bool OnHit(in RayCastResult hit)
        {
            Count++;
            return Count < StopAfter;
        }
    }

    [Test]
    public void ChannelFilterExcludesNonMatchingBodies()
    {
        var enemy = Bepu.CreateSphere(1f, At(new Vector3(0, 0, 5)), PhysicsState.Static);
        Bepu.SetCollisionChannel(enemy, 1);
        var scenery = Bepu.CreateSphere(1f, At(new Vector3(0, 0, 5)), PhysicsState.Static);
        Bepu.SetCollisionChannel(scenery, 2);

        var result = Bepu.RayCast(Vector3.Zero, Vector3.UnitZ, 20f, channel: 1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Body, Is.EqualTo(enemy));
    }

    [Test]
    public void SphereCastHitsSomethingARayWouldMiss()
    {
        // Offset to the side, just past a bare ray's path but within a wide swept sphere's radius.
        var target = Bepu.CreateSphere(0.5f, At(new Vector3(1.2f, 0, 5)), PhysicsState.Static);

        Assert.That(Bepu.RayCast(Vector3.Zero, Vector3.UnitZ, 20f), Is.Null, "sanity check: a bare ray should miss");

        var swept = Bepu.SphereCast(1f, Vector3.Zero, Vector3.UnitZ, 20f);
        Assert.That(swept, Is.Not.Null);
        Assert.That(swept!.Value.Body, Is.EqualTo(target));
    }

    [Test]
    public void OverlapSphereFindsBodyAtRestNoMotionNeeded()
    {
        var body = Bepu.CreateSphere(1f, At(new Vector3(5, 0, 0)), PhysicsState.Static);
        Bepu.CreateSphere(1f, At(new Vector3(50, 0, 0)), PhysicsState.Static);

        var hits = new PhysicsBodyHandle[4];
        var count = Bepu.OverlapSphere(2f, new Vector3(5, 0, 0), hits);

        Assert.That(hits.Take(count), Is.EquivalentTo(new[] { body }));
    }

    [Test]
    public void OverlapCapsuleRespectsChannelFilter()
    {
        var matching = Bepu.CreateSphere(0.5f, At(Vector3.Zero), PhysicsState.Static);
        Bepu.SetCollisionChannel(matching, 7);
        var other = Bepu.CreateSphere(0.5f, At(Vector3.Zero), PhysicsState.Static);
        Bepu.SetCollisionChannel(other, 9);

        var hits = new PhysicsBodyHandle[4];
        var count = Bepu.OverlapCapsule(1f, 1f, Vector3.Zero, Quaternion.Identity, hits, channel: 7);

        Assert.That(hits.Take(count), Is.EquivalentTo(new[] { matching }));
    }
}
