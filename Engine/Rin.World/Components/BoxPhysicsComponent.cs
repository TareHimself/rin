using System.Diagnostics;
using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Physics;

namespace Rin.World.Components;

public class BoxPhysicsComponent : SingleBodyPhysicsComponent
{
    private Vector3 _size;

    public Vector3 Size
    {
        get { return _size = PhysicsBody.IsValid ? Owner!.World!.PhysicsSystem.GetBoxSize(PhysicsBody) : _size; }

        set
        {
            _size = value;
            if (PhysicsBody.IsValid) Owner!.World!.PhysicsSystem.SetBoxSize(PhysicsBody, _size);
        }
    }

    protected override PhysicsBodyHandle CreateBody(in Transform transform, in PhysicsState state)
    {
        Debug.Assert(Owner?.World != null);
        return Owner.World.PhysicsSystem.CreateBox(Size, transform, state);
    }

    public override void ProcessHit(RayCastResult result)
    {
        throw new NotImplementedException();
    }
}
