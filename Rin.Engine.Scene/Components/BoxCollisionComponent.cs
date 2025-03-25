using System.Numerics;
using Rin.Engine.Scene.Physics;

namespace Rin.Engine.Scene.Components;

public class BoxCollisionComponent : CollisionComponent
{
    IPhysicsBox? _physicsBox;
    
    public Vector3 Size = new Vector3(30.0f);

    protected override IPhysicsBody CreatePhysicsBody()
    {
        return _physicsBox ??= Owner?.Scene?.GetPhysicsSystem()?.CreateBox(this, Size) ?? throw new InvalidOperationException();
    }
}