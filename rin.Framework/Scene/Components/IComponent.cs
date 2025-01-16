using rin.Framework.Core;
using rin.Framework.Scene.Actors;

namespace rin.Framework.Scene.Components;

public interface IComponent : ITickable
{
    public Actor? Owner { get; set; }
    
    public void Start();
    
    public void Stop();
}