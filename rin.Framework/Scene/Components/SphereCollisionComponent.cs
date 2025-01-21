using rin.Framework.Core.Math;
using rin.Framework.Scene.Physics;

namespace rin.Framework.Scene.Components;

public class SphereCollisionComponent : CollisionComponent
{
    IPhysicsSphere? _physicsSphere;
    
    public float Radius = 30.0f;

    protected override IPhysicsBody CreatePhysicsBody()
    {
        return _physicsSphere ??= Owner?.Scene?.GetPhysicsSystem()?.CreateSphere(this,Radius) ?? throw new InvalidOperationException();
    }
}