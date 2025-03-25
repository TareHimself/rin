using Rin.Engine.Scene.Actors;

namespace Rin.Engine.Scene.Components;

public class Component : IComponent
{
    public Actor? Owner { get; set; }
    public bool Active { get; protected set; }
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