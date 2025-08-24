using BepuPhysics;
using BepuPhysics.Collidables;
using Rin.Framework.Math;

namespace Rin.Engine.World.Physics.Bepu;

public class BepuCapsuleBody : BepuBody, IPhysicsCapsule
{
    private readonly TypedIndex _shapeIndex;
    private float _height;
    private float _radius;
    private Capsule _shape;

    public BepuCapsuleBody(PhysicsState state, Transform transform, BepuPhysicsSystem system, float radius,
        float halfHeight) : base(state, transform, system)
    {
        _radius = radius;
        _height = halfHeight * 2f;
        _shape = new Capsule(_radius * GetRadiusScale(), _height * GetHeightScale());
        _shapeIndex = System.Simulation.Shapes.Add(_shape);
    }

    public float GetRadius()
    {
        return _radius;
    }

    public float GetHalfHeight()
    {
        return _height;
    }

    public float GetScaledRadius()
    {
        return _shape.Radius;
    }

    public float GetScaledHalfHeight()
    {
        return _shape.HalfLength;
    }

    public void SetRadius(float radius)
    {
        _radius = radius;
        UpdateShape();
    }

    public void SetHalfHeight(float halfHeight)
    {
        _height = halfHeight * 2f;
        UpdateShape();
    }

    public void SetScaledRadius(float radius)
    {
        SetRadius(radius / GetRadiusScale());
    }

    public void SetScaledHalfHeight(float halfHeight)
    {
        SetHalfHeight(halfHeight / GetHeightScale());
    }

    protected override TypedIndex GetShapeIndex()
    {
        return _shapeIndex;
    }

    protected float GetRadiusScale()
    {
        var scale = GetScale();
        return float.Max(scale.X, scale.Z);
    }

    protected float GetHeightScale()
    {
        return GetScale().Y;
    }

    protected override void UpdateShape()
    {
        _shape.Radius = _radius * GetRadiusScale();
        _shape.Length = _height * GetHeightScale();
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