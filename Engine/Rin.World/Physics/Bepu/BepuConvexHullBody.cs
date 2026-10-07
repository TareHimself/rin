using System.Buffers;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Rin.Core.Shared.Math;

namespace Rin.World.Physics.Bepu;

internal class BepuConvexHullBody : BepuBody
{
    private readonly TypedIndex _shapeIndex;
    private readonly Vector3 _appliedScale;
    private ConvexHull _shape;

    private BepuConvexHullBody(PhysicsState state, Transform transform, BepuPhysicsSystem system, ConvexHull shape,
        Vector3 appliedScale) : base(state, transform, system)
    {
        _shape = shape;
        _appliedScale = appliedScale;
        _shapeIndex = System.Simulation.Shapes.Add(_shape);
    }

    public static BepuConvexHullBody Create(PhysicsState state, Transform transform, BepuPhysicsSystem system,
        ReadOnlySpan<Vector3> points)
    {
        if (points.Length < 4)
            throw new ArgumentException("A convex hull needs at least 4 points", nameof(points));

        var scaled = ArrayPool<Vector3>.Shared.Rent(points.Length);
        try
        {
            for (var i = 0; i < points.Length; i++) scaled[i] = points[i] * transform.Scale;
            var shape = new ConvexHull(scaled.AsSpan(0, points.Length), system.Simulation.BufferPool, out var center);
            transform.Position += Vector3.Transform(center, transform.Orientation);
            return new BepuConvexHullBody(state, transform, system, shape, transform.Scale);
        }
        finally
        {
            ArrayPool<Vector3>.Shared.Return(scaled);
        }
    }

    protected override TypedIndex GetShapeIndex()
    {
        return _shapeIndex;
    }

    protected override void UpdateShape()
    {
        UpdateInertia();
    }

    protected override void ScaleUpdated()
    {
        if (GetScale() != _appliedScale)
            throw new NotSupportedException("A convex hull's scale is fixed at creation");
    }

    protected override BodyInertia ComputeInertia()
    {
        return _shape.ComputeInertia(GetMass());
    }
}
