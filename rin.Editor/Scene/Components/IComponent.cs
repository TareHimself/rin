using rin.Framework.Core;
using rin.Editor.Scene.Actors;

namespace rin.Editor.Scene.Components;

public interface IComponent : ITickable
{
    public Actor? Owner { get; set; }
    
    public void Start();
    
    public void Stop();
}