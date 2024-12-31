
using rin.Framework.World.Entities;

namespace rin.Framework.World.Components;

public class Component : IDisposable
{
    public Entity? Owner { get; set; }

    public virtual void Init()
    {
        
    }
    public virtual void Tick(double delta)
    {
        
    }
    public virtual void Dispose()
    {
        
    }
}