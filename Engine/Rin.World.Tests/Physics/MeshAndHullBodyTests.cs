using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;

namespace Rin.World.Tests.Physics;

public class MeshAndHullBodyTests : BepuFixture
{
    private static readonly Vector3[] FloorVertices =
        [new(-10, 0, -10), new(10, 0, -10), new(10, 0, 10), new(-10, 0, 10)];

    private static readonly uint[] FloorIndices = [0, 1, 2, 0, 2, 3];

    private static readonly Vector3[] CubePoints =
    [
        new(-1, -1, -1), new(1, -1, -1), new(1, 1, -1), new(-1, 1, -1),
        new(-1, -1, 1), new(1, -1, 1), new(1, 1, 1), new(-1, 1, 1)
    ];

    [Test]
    public void StaticMeshIsHitByRayFromAbove()
    {
        var floor = Bepu.CreateStaticMesh(FloorVertices, FloorIndices, At(Vector3.Zero));

        var result = Bepu.RayCast(new Vector3(0, 5, 0), -Vector3.UnitY, 20f);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Body, Is.EqualTo(floor));
        Assert.That(result.Value.Distance, Is.EqualTo(5f).Within(0.01f));
        Assert.That(Bepu.GetState(floor), Is.EqualTo(PhysicsState.Static));
    }

    [Test]
    public void StaticMeshStopsFallingSphere()
    {
        Bepu.CreateStaticMesh(FloorVertices, FloorIndices, At(Vector3.Zero));
        var ball = Bepu.CreateSphere(0.5f, At(new Vector3(0, 3, 0)), PhysicsState.Simulated);

        for (var i = 0; i < 240; i++) Bepu.Update(1f / 60f);

        Assert.That(Bepu.GetPosition(ball).Y, Is.EqualTo(0.5f).Within(0.1f));
    }

    [Test]
    public void StaticMeshRejectsIndexCountNotMultipleOfThree()
    {

        Assert.Throws<ArgumentException>(() => Bepu.CreateStaticMesh(FloorVertices, [0, 1], At(Vector3.Zero)));
    }

    [Test]
    public void StaticMeshRejectsOutOfRangeIndex()
    {

        Assert.Throws<ArgumentException>(() => Bepu.CreateStaticMesh(FloorVertices, [0, 1, 9], At(Vector3.Zero)));
    }

    [Test]
    public void StaticMeshScaleIsFixedAtCreation()
    {
        var floor = Bepu.CreateStaticMesh(FloorVertices, FloorIndices, At(Vector3.Zero));

        Assert.Throws<NotSupportedException>(() => Bepu.SetScale(floor, new Vector3(2, 2, 2)));
    }

    [Test]
    public void ConvexHullIsHitByRay()
    {
        var hull = Bepu.CreateConvexHull(CubePoints, At(new Vector3(0, 0, 5)), PhysicsState.Static);

        var result = Bepu.RayCast(Vector3.Zero, Vector3.UnitZ, 20f);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Body, Is.EqualTo(hull));
        Assert.That(result.Value.Distance, Is.EqualTo(4f).Within(0.01f));
    }

    [Test]
    public void SimulatedConvexHullFallsOntoStaticMesh()
    {
        Bepu.CreateStaticMesh(FloorVertices, FloorIndices, At(Vector3.Zero));
        var crate = Bepu.CreateConvexHull(CubePoints, At(new Vector3(0, 4, 0)), PhysicsState.Simulated);

        for (var i = 0; i < 240; i++) Bepu.Update(1f / 60f);

        Assert.That(Bepu.GetPosition(crate).Y, Is.EqualTo(1f).Within(0.1f));
    }

    [Test]
    public void ConvexHullPlacesPointsAtRequestedWorldPosition()
    {
        Vector3[] offsetCube = [.. CubePoints.Select(p => p + new Vector3(10, 0, 0))];
        Bepu.CreateConvexHull(offsetCube, At(Vector3.Zero), PhysicsState.Static);

        var result = Bepu.RayCast(new Vector3(10, 0, -5), Vector3.UnitZ, 20f);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Distance, Is.EqualTo(4f).Within(0.01f));
    }

    [Test]
    public void ConvexHullRejectsTooFewPoints()
    {

        Assert.Throws<ArgumentException>(() =>
            Bepu.CreateConvexHull(CubePoints.AsSpan(0, 3), At(Vector3.Zero), PhysicsState.Static));
    }

    [Test]
    public void DestroyedMeshAndHullAreNoLongerHit()
    {
        var floor = Bepu.CreateStaticMesh(FloorVertices, FloorIndices, At(Vector3.Zero));
        var hull = Bepu.CreateConvexHull(CubePoints, At(new Vector3(0, 5, 0)), PhysicsState.Static);

        Bepu.DestroyBody(floor);
        Bepu.DestroyBody(hull);

        Assert.That(Bepu.RayCast(new Vector3(0, 20, 0), -Vector3.UnitY, 40f), Is.Null);
    }
}
