using Rin.Engine.World.Physics;

namespace Rin.Engine.World.Components;

public class CapsuleCollisionComponent : CollisionComponent
{
    private IPhysicsCapsule? _physicsCapsule;
    public float HalfHeight = 50.0f;

    public float Radius = 30.0f;

    protected override IPhysicsBody CreatePhysicsBody()
    {
        return _physicsCapsule ??= Owner?.World?.GetPhysicsSystem()?.CreateCapsule(this, Radius, HalfHeight) ??
                                   throw new InvalidOperationException();
    }
}