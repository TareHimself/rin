using rin.Framework.Core.Math;
using rin.Editor.Scene.Physics;

namespace rin.Editor.Scene.Components;

public class SphereCollisionComponent : CollisionComponent
{
    IPhysicsSphere? _physicsSphere;
    
    public float Radius = 30.0f;

    protected override IPhysicsBody CreatePhysicsBody()
    {
        return _physicsSphere ??= Owner?.Scene?.GetPhysicsSystem()?.CreateSphere(this,Radius) ?? throw new InvalidOperationException();
    }
}