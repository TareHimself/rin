using Rin.Engine.World.Physics;

namespace Rin.Engine.World.Components;

public class SphereCollisionComponent : CollisionComponent
{
    private IPhysicsSphere? _physicsSphere;

    public float Radius = 30.0f;

    protected override IPhysicsBody CreatePhysicsBody()
    {
        return _physicsSphere ??= Owner?.World?.GetPhysicsSystem()?.CreateSphere(this, Radius) ??
                                  throw new InvalidOperationException();
    }
}