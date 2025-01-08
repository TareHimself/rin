using rin.Framework.Scene.Entities;

namespace rin.Framework.Scene.Components;

public class Component : IComponent
{
    public Entity? Owner { get; set; }
    public virtual void Init()
    {
    }

    public virtual void Destroy()
    {
    }

    public void Tick(double delta)
    {
        
    }
}