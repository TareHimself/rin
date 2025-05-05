using Rin.Engine.World.Physics;

namespace Rin.Engine.World.Components;

public interface IPhysicsComponent : ISceneComponent
{
    public void PrePhysicsUpdate();
    public void ProcessHit(IPhysicsBody body, RayCastResult result);
}