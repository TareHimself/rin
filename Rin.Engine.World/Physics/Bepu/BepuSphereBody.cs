using BepuPhysics;
using BepuPhysics.Collidables;
using Rin.Engine.Math;

namespace Rin.Engine.World.Physics.Bepu;

public class BepuSphereBody : BepuBody, IPhysicsSphere
{
    private readonly TypedIndex _shapeIndex;
    private float _radius;
    private Sphere _shape;

    public BepuSphereBody(PhysicsState state, Transform transform, BepuPhysicsSystem system, float radius) : base(state,
        transform, system)
    {
        _radius = radius;
        _shape = new Sphere(_radius * GetRadiusScale());
        _shapeIndex = System.Simulation.Shapes.Add(_shape);
    }

    public float GetRadius()
    {
        return _radius;
    }

    public float GetScaledRadius()
    {
        return _shape.Radius;
    }

    public void SetRadius(float radius)
    {
        _radius = radius;
        UpdateShape();
    }

    public void SetScaledRadius(float radius)
    {
        SetRadius(radius / GetRadiusScale());
    }

    private float GetRadiusScale()
    {
        var scale = GetScale();
        return float.Max(scale.X, float.Max(scale.Y, scale.Z));
    }

    protected override TypedIndex GetShapeIndex()
    {
        return _shapeIndex;
    }

    protected override void UpdateShape()
    {
        _shape.Radius = _radius * GetRadiusScale();
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