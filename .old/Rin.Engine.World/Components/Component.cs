using Rin.Engine.World.Actors;

namespace Rin.Engine.World.Components;

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
}