using rin.Framework.Core;
using rin.Editor.Scene.Components;

namespace rin.Editor.Scene.Systems;

public interface ISystem : ITickable
{
    
    public bool Tickable { get; }
    public void Startup(Scene scene);
    public void Shutdown(Scene scene);
    public void OnComponentCreated(IComponent component);
    public void OnComponentDestroyed(IComponent component);
}