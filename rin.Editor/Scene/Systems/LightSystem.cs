using rin.Editor.Scene.Components;
using rin.Editor.Scene.Components.Lights;

namespace rin.Editor.Scene.Systems;

public class LightSystem : ISystem
{
    private readonly HashSet<LightComponent> _lights = [];
    
    public LightComponent[] GetLights()
    {
        lock (_lights)
        {
            return _lights.ToArray();
        }
    }
    
    public bool Tickable => false;

    public void Startup(Scene scene)
    {

    }

    public void Shutdown(Scene scene)
    {
        
    }

    public void Update(double deltaSeconds)
    {
  
    }

    public void OnComponentCreated(IComponent component)
    {
        if (component is LightComponent asLightComponent)
        {
            lock (_lights)
            {
                _lights.Add(asLightComponent);
            }
        }
    }

    public void OnComponentDestroyed(IComponent component)
    {
        if (component is LightComponent asLightComponent)
        {
            lock (_lights)
            {
                _lights.Remove(asLightComponent);
            }
        }
    }
}