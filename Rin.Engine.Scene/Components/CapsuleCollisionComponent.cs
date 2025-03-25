using Rin.Engine.Scene.Physics;

namespace Rin.Engine.Scene.Components;

public class CapsuleCollisionComponent : CollisionComponent
{
    IPhysicsCapsule? _physicsCapsule;
    
    public float Radius = 30.0f;
    public float HalfHeight = 50.0f;

    protected override IPhysicsBody CreatePhysicsBody()
    {
        return _physicsCapsule ??= Owner?.Scene?.GetPhysicsSystem()?.CreateCapsule(this,Radius,HalfHeight) ?? throw new InvalidOperationException();
    }
}