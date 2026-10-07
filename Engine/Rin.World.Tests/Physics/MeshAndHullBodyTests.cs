using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;

namespace Rin.World.Tests.Physics;

public class MeshAndHullBodyTests
{
    private static readonly Vector3[] FloorVertices =
        [new(-10, 0, -10), new(10, 0, -10), new(10, 0, 10), new(-10, 0, 10)];

    private static readonly uint[] FloorIndices = [0, 1, 2, 0, 2, 3];

    private static readonly Vector3[] CubePoints =
    [
        new(-1, -1, -1), new(1, -1, -1), new(1, 1, -1), new(-1, 1, -1),
        new(-1, -1, 1), new(1, -1, 1), new(1, 1, 1), new(-1, 1, 1)
    ];

    private static Transform At(Vector3 position) =>
        new() { Position = position, Orientation = Quaternion.Identity, Scale = Vector3.One };

    [Test]
    public void StaticMeshIsHitByRayFromAbove()
    {
        var physics = new BepuPhysicsSystem();
        var floor = physics.CreateStaticMesh(FloorVertices, FloorIndices, At(Vector3.Zero));

        var result = physics.RayCast(new Vector3(0, 5, 0), -Vector3.UnitY, 20f);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Body, Is.EqualTo(floor));
        Assert.That(result.Value.Distance, Is.EqualTo(5f).Within(0.01f));
        Assert.That(physics.GetState(floor), Is.EqualTo(PhysicsState.Static));
    }

    [Test]
    public void StaticMeshStopsFallingSphere()
    {
        var physics = new BepuPhysicsSystem();
        physics.CreateStaticMesh(FloorVertices, FloorIndices, At(Vector3.Zero));
        var ball = physics.CreateSphere(0.5f, At(new Vector3(0, 3, 0)), PhysicsState.Simulated);

        for (var i = 0; i < 240; i++) physics.Update(1f / 60f);

        Assert.That(physics.GetPosition(ball).Y, Is.EqualTo(0.5f).Within(0.1f));
    }

    [Test]
    public void StaticMeshRejectsIndexCountNotMultipleOfThree()
    {
        var physics = new BepuPhysicsSystem();

        Assert.Throws<ArgumentException>(() => physics.CreateStaticMesh(FloorVertices, [0, 1], At(Vector3.Zero)));
    }

    [Test]
    public void StaticMeshRejectsOutOfRangeIndex()
    {
        var physics = new BepuPhysicsSystem();

        Assert.Throws<ArgumentException>(() => physics.CreateStaticMesh(FloorVertices, [0, 1, 9], At(Vector3.Zero)));
    }

    [Test]
    public void StaticMeshScaleIsFixedAtCreation()
    {
        var physics = new BepuPhysicsSystem();
        var floor = physics.CreateStaticMesh(FloorVertices, FloorIndices, At(Vector3.Zero));

        Assert.Throws<NotSupportedException>(() => physics.SetScale(floor, new Vector3(2, 2, 2)));
    }

    [Test]
    public void ConvexHullIsHitByRay()
    {
        var physics = new BepuPhysicsSystem();
        var hull = physics.CreateConvexHull(CubePoints, At(new Vector3(0, 0, 5)), PhysicsState.Static);

        var result = physics.RayCast(Vector3.Zero, Vector3.UnitZ, 20f);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Body, Is.EqualTo(hull));
        Assert.That(result.Value.Distance, Is.EqualTo(4f).Within(0.01f));
    }

    [Test]
    public void SimulatedConvexHullFallsOntoStaticMesh()
    {
        var physics = new BepuPhysicsSystem();
        physics.CreateStaticMesh(FloorVertices, FloorIndices, At(Vector3.Zero));
        var crate = physics.CreateConvexHull(CubePoints, At(new Vector3(0, 4, 0)), PhysicsState.Simulated);

        for (var i = 0; i < 240; i++) physics.Update(1f / 60f);

        Assert.That(physics.GetPosition(crate).Y, Is.EqualTo(1f).Within(0.1f));
    }

    [Test]
    public void ConvexHullPlacesPointsAtRequestedWorldPosition()
    {
        var physics = new BepuPhysicsSystem();
        Vector3[] offsetCube = [.. CubePoints.Select(p => p + new Vector3(10, 0, 0))];
        physics.CreateConvexHull(offsetCube, At(Vector3.Zero), PhysicsState.Static);

        var result = physics.RayCast(new Vector3(10, 0, -5), Vector3.UnitZ, 20f);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Distance, Is.EqualTo(4f).Within(0.01f));
    }

    [Test]
    public void ConvexHullRejectsTooFewPoints()
    {
        var physics = new BepuPhysicsSystem();

        Assert.Throws<ArgumentException>(() =>
            physics.CreateConvexHull(CubePoints.AsSpan(0, 3), At(Vector3.Zero), PhysicsState.Static));
    }

    [Test]
    public void DestroyedMeshAndHullAreNoLongerHit()
    {
        var physics = new BepuPhysicsSystem();
        var floor = physics.CreateStaticMesh(FloorVertices, FloorIndices, At(Vector3.Zero));
        var hull = physics.CreateConvexHull(CubePoints, At(new Vector3(0, 5, 0)), PhysicsState.Static);

        physics.DestroyBody(floor);
        physics.DestroyBody(hull);

        Assert.That(physics.RayCast(new Vector3(0, 20, 0), -Vector3.UnitY, 40f), Is.Null);
    }
}
