using System.Numerics;
using rin.Framework.Core.Math;
using rin.Editor.Scene.Physics;

namespace rin.Editor.Scene.Components;

public class BoxCollisionComponent : CollisionComponent
{
    IPhysicsBox? _physicsBox;
    
    public Vector3 Size = new Vector3(30.0f);

    protected override IPhysicsBody CreatePhysicsBody()
    {
        return _physicsBox ??= Owner?.Scene?.GetPhysicsSystem()?.CreateBox(this, Size) ?? throw new InvalidOperationException();
    }
}