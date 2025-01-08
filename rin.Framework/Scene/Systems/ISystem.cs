using rin.Framework.Core;
using rin.Framework.Scene.Components;

namespace rin.Framework.Scene.Systems;

public interface ISystem : ITickable
{
    
    public bool Tickable { get; }
    public void Startup(Scene scene);
    public void Shutdown(Scene scene);
    public void OnComponentCreated(IComponent component);
    public void OnComponentDestroyed(IComponent component);
}