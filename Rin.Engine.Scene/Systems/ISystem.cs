using Rin.Engine.Core;
using Rin.Engine.Scene.Components;

namespace Rin.Engine.Scene.Systems;

public interface ISystem : IReceivesUpdate
{
    
    public bool Tickable { get; }
    public void Startup(Scene scene);
    public void Shutdown(Scene scene);
    public void OnComponentCreated(IComponent component);
    public void OnComponentDestroyed(IComponent component);
}