using System.Numerics;
using Rin.Engine.World.Physics;

namespace Rin.Engine.World.Components;

public class BoxCollisionComponent : CollisionComponent
{
    IPhysicsBox? _physicsBox;
    
    public Vector3 Size = new Vector3(30.0f);

    protected override IPhysicsBody CreatePhysicsBody()
    {
        return _physicsBox ??= Owner?.World?.GetPhysicsSystem()?.CreateBox(this, Size) ?? throw new InvalidOperationException();
    }
}