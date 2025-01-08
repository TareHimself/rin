using rin.Framework.Core;
using rin.Framework.Scene.Entities;

namespace rin.Framework.Scene.Components;

public interface IComponent : ITickable
{
    public Entity? Owner { get; set; }
    
    public void Init();
    
    public void Destroy();
}