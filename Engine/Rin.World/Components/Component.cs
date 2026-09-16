using Rin.World.Actors;
using Rin.World.Physics;

namespace Rin.World.Components;

public class Component : IComponent
{
    public bool Active { get; protected set; }
    public Actor? Owner { get; set; }


    public virtual void Start()
    {
        Active = true;
    }

    public virtual void Stop()
    {
        Active = false;
    }

    public virtual void Update(float deltaSeconds)
    {
    }

    public virtual void LateUpdate(float deltaSeconds)
    {
    }

    public virtual void PrePhysicsUpdate()
    {
    }

    public virtual void ProcessHit(RayCastResult result)
    {
    }
}
