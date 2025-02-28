using System.Numerics;
using Rin.Editor.Scene.Physics;
using Rin.Engine.Core.Math;

namespace Rin.Editor.Scene.Components;

public class BoxCollisionComponent : CollisionComponent
{
    IPhysicsBox? _physicsBox;
    
    public Vector3 Size = new Vector3(30.0f);

    protected override IPhysicsBody CreatePhysicsBody()
    {
        return _physicsBox ??= Owner?.Scene?.GetPhysicsSystem()?.CreateBox(this, Size) ?? throw new InvalidOperationException();
    }
}