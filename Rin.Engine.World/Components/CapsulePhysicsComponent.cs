using System.Diagnostics;
using Rin.Engine.World.Physics;
using Rin.Framework.Shared.Math;

namespace Rin.Engine.World.Components;

public class CapsulePhysicsComponent : SingleBodyPhysicsComponent
{
    private float _halfHeight = 10.0f;
    private IPhysicsCapsule? _physicsCapsule;
    private float _radius = 5.0f;

    public float HalfHeight
    {
        get { return _halfHeight = _physicsCapsule?.GetHalfHeight() ?? _halfHeight; }
        set
        {
            _halfHeight = value;
            _physicsCapsule?.SetHalfHeight(_radius);
        }
    }

    public float Radius
    {
        get { return _radius = _physicsCapsule?.GetRadius() ?? _radius; }
        set
        {
            _radius = value;
            _physicsCapsule?.SetRadius(_radius);
        }
    }

    protected override IPhysicsBody CreateBody(in Transform transform, in PhysicsState state)
    {
        Debug.Assert(Owner?.World != null);
        _physicsCapsule = Owner.World.GetPhysicsSystem().CreateCapsule(_radius, _halfHeight, transform, state);
        return _physicsCapsule;
    }

    public override void ProcessHit(IPhysicsBody body, RayCastResult result)
    {
        throw new NotImplementedException();
    }
}