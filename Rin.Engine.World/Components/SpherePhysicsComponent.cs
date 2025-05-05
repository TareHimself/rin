using System.Diagnostics;
using Rin.Engine.Math;
using Rin.Engine.World.Physics;

namespace Rin.Engine.World.Components;

public class SpherePhysicsComponent : SingleBodyPhysicsComponent
{
    private IPhysicsSphere? _physicsSphere;
    private float _radius = 5f;

    public float Radius
    {
        get { return _radius = _physicsSphere?.GetRadius() ?? _radius; }
        set
        {
            _radius = value;
            _physicsSphere?.SetRadius(_radius);
        }
    }

    protected override IPhysicsBody CreateBody(in Transform transform, in PhysicsState state)
    {
        Debug.Assert(Owner?.World != null);
        _physicsSphere = Owner.World.GetPhysicsSystem().CreateSphere(_radius, transform, state);
        return _physicsSphere;
    }

    public override void ProcessHit(IPhysicsBody body, RayCastResult result)
    {
        throw new NotImplementedException();
    }
}