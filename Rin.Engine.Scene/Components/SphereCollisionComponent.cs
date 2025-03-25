using Rin.Engine.Scene.Physics;

namespace Rin.Engine.Scene.Components;

public class SphereCollisionComponent : CollisionComponent
{
    IPhysicsSphere? _physicsSphere;
    
    public float Radius = 30.0f;

    protected override IPhysicsBody CreatePhysicsBody()
    {
        return _physicsSphere ??= Owner?.Scene?.GetPhysicsSystem()?.CreateSphere(this,Radius) ?? throw new InvalidOperationException();
    }
}