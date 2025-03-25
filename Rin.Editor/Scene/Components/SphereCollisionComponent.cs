using Rin.Editor.Scene.Physics;
using Rin.Engine.Core.Math;

namespace Rin.Editor.Scene.Components;

public class SphereCollisionComponent : CollisionComponent
{
    IPhysicsSphere? _physicsSphere;
    
    public float Radius = 30.0f;

    protected override IPhysicsBody CreatePhysicsBody()
    {
        return _physicsSphere ??= Owner?.Scene?.GetPhysicsSystem()?.CreateSphere(this,Radius) ?? throw new InvalidOperationException();
    }
}