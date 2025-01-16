using rin.Framework.Core.Math;
using rin.Framework.Scene.Physics;

namespace rin.Framework.Scene.Components;

public class BoxCollisionComponent : CollisionComponent
{
    IPhysicsBox? _physicsBox;
    
    public Vec3<float> Size = new Vec3<float>(30.0f);

    protected override IPhysicsBody CreatePhysicsBody()
    {
        return _physicsBox ??= Owner?.Scene?.GetPhysicsSystem()?.CreateBox(this, Size) ?? throw new InvalidOperationException();
    }
}