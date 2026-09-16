using System.Diagnostics;
using Rin.Core.Shared.Math;
using Rin.World.Physics;

namespace Rin.World.Components;

public class SpherePhysicsComponent : SingleBodyPhysicsComponent
{
    private float _radius = 5f;

    public float Radius
    {
        get { return _radius = PhysicsBody.IsValid ? Owner!.World!.PhysicsSystem.GetSphereRadius(PhysicsBody) : _radius; }
        set
        {
            _radius = value;
            if (PhysicsBody.IsValid) Owner!.World!.PhysicsSystem.SetSphereRadius(PhysicsBody, _radius);
        }
    }

    protected override PhysicsBodyHandle CreateBody(in Transform transform, in PhysicsState state)
    {
        Debug.Assert(Owner?.World != null);
        return Owner.World.PhysicsSystem.CreateSphere(_radius, transform, state);
    }

    public override void ProcessHit(RayCastResult result)
    {
        throw new NotImplementedException();
    }
}
