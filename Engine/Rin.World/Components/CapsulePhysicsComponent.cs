using System.Diagnostics;
using Rin.Core.Shared.Math;
using Rin.World.Physics;

namespace Rin.World.Components;

public class CapsulePhysicsComponent : SingleBodyPhysicsComponent
{
    private float _halfHeight = 10.0f;
    private float _radius = 5.0f;

    public float HalfHeight
    {
        get { return _halfHeight = PhysicsBody.IsValid ? Owner!.World!.PhysicsSystem.GetCapsuleHalfHeight(PhysicsBody) : _halfHeight; }
        set
        {
            _halfHeight = value;
            if (PhysicsBody.IsValid) Owner!.World!.PhysicsSystem.SetCapsuleHalfHeight(PhysicsBody, _halfHeight);
        }
    }

    public float Radius
    {
        get { return _radius = PhysicsBody.IsValid ? Owner!.World!.PhysicsSystem.GetCapsuleRadius(PhysicsBody) : _radius; }
        set
        {
            _radius = value;
            if (PhysicsBody.IsValid) Owner!.World!.PhysicsSystem.SetCapsuleRadius(PhysicsBody, _radius);
        }
    }

    protected override PhysicsBodyHandle CreateBody(in Transform transform, in PhysicsState state)
    {
        Debug.Assert(Owner?.World != null);
        return Owner.World.PhysicsSystem.CreateCapsule(_radius, _halfHeight, transform, state);
    }

    public override void ProcessHit(RayCastResult result)
    {
        throw new NotImplementedException();
    }
}
