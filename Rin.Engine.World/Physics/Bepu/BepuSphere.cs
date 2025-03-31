using BepuPhysics;
using BepuPhysics.Collidables;
using Rin.Engine.World.Components;

namespace Rin.Engine.World.Physics.Bepu;

public class BepuSphere : BepuBody, IPhysicsSphere
{

    private float _radius;
    public float Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            UpdateShape();
        }
    }

    private Sphere _shape;
    private readonly TypedIndex _typedIndex;

    public BepuSphere(BepuPhysics physics,IPhysicsComponent owner, float radius) : base(physics,owner)
    {
        _radius = radius;
        _shape = CreateShape();
        _typedIndex = physics.Simulation.Shapes.Add(_shape);
    }

    private void UpdateShape()
    {
        _shape = CreateShape();
        Physics.Simulation.Shapes.GetShape<Sphere>(_typedIndex.Index) = _shape;
    }

    private Sphere CreateShape()
    {
        return new Sphere()
        {
            Radius = _radius,
        };
    }

    protected override TypedIndex GetTypedIndex() => _typedIndex;

    protected override BodyInertia ComputeInertia() => _shape.ComputeInertia(Mass);

    public override void Dispose()
    {
        base.Dispose();
        Physics.Simulation.Shapes.Remove(_typedIndex);
    }
}