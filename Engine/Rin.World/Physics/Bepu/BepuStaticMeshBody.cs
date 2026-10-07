using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Rin.Core.Shared.Math;

namespace Rin.World.Physics.Bepu;

internal class BepuStaticMeshBody : BepuBody
{
    private readonly TypedIndex _shapeIndex;
    private readonly Vector3 _appliedScale;

    public BepuStaticMeshBody(Transform transform, BepuPhysicsSystem system, ReadOnlySpan<Vector3> vertices,
        ReadOnlySpan<uint> indices) : base(PhysicsState.Static, transform, system)
    {
        if (indices.Length % 3 != 0)
            throw new ArgumentException("Index count must be a multiple of 3", nameof(indices));

        var pool = system.Simulation.BufferPool;
        pool.Take<Triangle>(indices.Length / 3, out var triangles);
        for (var i = 0; i < triangles.Length; i++)
            triangles[i] = new Triangle(Vertex(vertices, indices[i * 3]), Vertex(vertices, indices[i * 3 + 1]),
                Vertex(vertices, indices[i * 3 + 2]));

        _appliedScale = transform.Scale;
        _shapeIndex = System.Simulation.Shapes.Add(new BepuPhysics.Collidables.Mesh(triangles, _appliedScale, pool));
    }

    protected override TypedIndex GetShapeIndex()
    {
        return _shapeIndex;
    }

    protected override void UpdateShape()
    {
    }

    protected override void ScaleUpdated()
    {
        if (GetScale() != _appliedScale)
            throw new NotSupportedException("A static mesh's scale is fixed at creation");
    }

    protected override BodyInertia ComputeInertia()
    {
        return default;
    }

    private static Vector3 Vertex(ReadOnlySpan<Vector3> vertices, uint index)
    {
        if (index >= vertices.Length)
            throw new ArgumentException($"Index {index} is outside the {vertices.Length} vertices", nameof(vertices));
        return vertices[(int)index];
    }
}
