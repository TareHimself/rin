using rin.Framework.Scene.Components;

namespace rin.Framework.Scene.Systems;

public interface ISystem
{
    
    public bool WillTick { get; }
    public void Startup(Scene scene);
    public void Shutdown(Scene scene);
    public void Tick(double delta);
    public void OnComponentCreated(IComponent component);
    public void OnComponentDestroyed(IComponent component);
}