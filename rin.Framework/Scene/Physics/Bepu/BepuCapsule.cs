using BepuPhysics;
using BepuPhysics.Collidables;
using rin.Framework.Core.Math;
using rin.Framework.Scene.Components;

namespace rin.Framework.Scene.Physics.Bepu;

public class BepuCapsule : BepuBody, IPhysicsCapsule
{

    private float _radius;
    private float _halfHeight;
    
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

    private Capsule _shape;
    private readonly TypedIndex _typedIndex;

    public BepuCapsule(BepuPhysics physics,IPhysicsComponent owner, float radius,float halfHeight) : base(physics,owner)
    {
        _radius = radius;
        _shape = CreateShape();
        _typedIndex = physics.Simulation.Shapes.Add(_shape);
    }

    private void UpdateShape()
    {
        _shape = CreateShape();
        Physics.Simulation.Shapes.GetShape<Capsule>(_typedIndex.Index) = _shape;
    }

    private Capsule CreateShape()
    {
        return new Capsule()
        {
            Radius = _radius,
            HalfLength = _halfHeight
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