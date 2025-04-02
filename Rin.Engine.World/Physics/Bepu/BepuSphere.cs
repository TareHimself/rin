using BepuPhysics;
using BepuPhysics.Collidables;
using Rin.Engine.World.Components;

namespace Rin.Engine.World.Physics.Bepu;

public class BepuSphere : BepuBody, IPhysicsSphere
{
    private readonly TypedIndex _typedIndex;

    private float _radius;

    private Sphere _shape;

    public BepuSphere(BepuPhysics physics, IPhysicsComponent owner, float radius) : base(physics, owner)
    {
        _radius = radius;
        _shape = CreateShape();
        _typedIndex = physics.Simulation.Shapes.Add(_shape);
    }

    public float Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            UpdateShape();
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        Physics.Simulation.Shapes.Remove(_typedIndex);
    }

    private void UpdateShape()
    {
        _shape = CreateShape();
        Physics.Simulation.Shapes.GetShape<Sphere>(_typedIndex.Index) = _shape;
    }

    private Sphere CreateShape()
    {
        return new Sphere
        {
            Radius = _radius
        };
    }

    protected override TypedIndex GetTypedIndex()
    {
        return _typedIndex;
    }

    protected override BodyInertia ComputeInertia()
    {
        return _shape.ComputeInertia(Mass);
    }
}