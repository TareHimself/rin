using System.Diagnostics;
using System.Numerics;
using Rin.Engine.World.Physics;
using Rin.Framework.Shared.Math;

namespace Rin.Engine.World.Components;

public class BoxPhysicsComponent : SingleBodyPhysicsComponent
{
    private IPhysicsBox? _physicsBox;
    private Vector3 _size;

    public Vector3 Size
    {
        get { return _size = _physicsBox?.GetSize() ?? _size; }

        set
        {
            _size = value;
            _physicsBox?.SetSize(_size);
        }
    }

    protected override IPhysicsBody CreateBody(in Transform transform, in PhysicsState state)
    {
        Debug.Assert(Owner?.World != null);
        _physicsBox = Owner.World.GetPhysicsSystem().CreateBox(Size, transform, state);
        return _physicsBox;
    }

    public override void ProcessHit(IPhysicsBody body, RayCastResult result)
    {
        throw new NotImplementedException();
    }
}