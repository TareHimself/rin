using Rin.Editor.Scene.Components;
using Rin.Engine.Core;

namespace Rin.Editor.Scene.Systems;

public interface ISystem : IReceivesUpdate
{
    
    public bool Tickable { get; }
    public void Startup(Scene scene);
    public void Shutdown(Scene scene);
    public void OnComponentCreated(IComponent component);
    public void OnComponentDestroyed(IComponent component);
}