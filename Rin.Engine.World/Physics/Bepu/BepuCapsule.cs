using BepuPhysics;
using BepuPhysics.Collidables;
using Rin.Engine.World.Components;

namespace Rin.Engine.World.Physics.Bepu;

public class BepuCapsule : BepuBody, IPhysicsCapsule
{
    private readonly TypedIndex _typedIndex;
    private float _halfHeight;

    private float _radius;

    private Capsule _shape;

    public BepuCapsule(BepuPhysics physics, IPhysicsComponent owner, float radius, float halfHeight) : base(physics,
        owner)
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

    public float HalfHeight
    {
        get => _halfHeight;
        set
        {
            _halfHeight = value;
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
        Physics.Simulation.Shapes.GetShape<Capsule>(_typedIndex.Index) = _shape;
    }

    private Capsule CreateShape()
    {
        return new Capsule
        {
            Radius = _radius,
            HalfLength = _halfHeight
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