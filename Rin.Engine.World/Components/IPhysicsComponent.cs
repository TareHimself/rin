using Rin.Engine.World.Physics;

namespace Rin.Engine.World.Components;

public interface IPhysicsComponent : IWorldComponent
{
    public void PrePhysicsUpdate();
    public void ProcessHit(IPhysicsBody body, RayCastResult result);
}