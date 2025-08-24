using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Rin.Framework.Math;

namespace Rin.Engine.World.Physics.Bepu;

public class BepuBoxBody : BepuBody, IPhysicsBox
{
    private readonly TypedIndex _shapeIndex;
    private Box _shape;
    private Vector3 _size;

    public BepuBoxBody(PhysicsState state, Transform transform, BepuPhysicsSystem system, in Vector3 size) : base(state,
        transform, system)
    {
        _size = size;
        var scaledSize = _size * GetScale();
        _shape = new Box(scaledSize.X, scaledSize.Y, scaledSize.Z);
        _shapeIndex = System.Simulation.Shapes.Add(_shape);
    }

    public Vector3 GetSize()
    {
        return _size;
    }

    public void SetSize(in Vector3 size)
    {
        _size = size;
        UpdateShape();
    }

    public Vector3 GetScaledSize()
    {
        return _size * GetScale();
    }

    public void SetScaledSize(in Vector3 size)
    {
        SetSize(size / GetScale());
    }

    protected override TypedIndex GetShapeIndex()
    {
        return _shapeIndex;
    }

    protected override void UpdateShape()
    {
        var scaledSize = _size * GetScale();
        _shape.Width = scaledSize.X;
        _shape.Height = scaledSize.Y;
        _shape.Length = scaledSize.Z;
        UpdateInertia();
    }

    protected override void ScaleUpdated()
    {
        UpdateShape();
    }

    protected override BodyInertia ComputeInertia()
    {
        return _shape.ComputeInertia(GetMass());
    }
}