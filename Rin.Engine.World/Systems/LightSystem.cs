using Rin.Engine.World.Components;
using Rin.Engine.World.Components.Lights;

namespace Rin.Engine.World.Systems;

public class LightSystem : ISystem
{
    private readonly HashSet<LightComponent> _lights = [];

    public bool Tickable => false;

    public void Startup(World world)
    {
    }

    public void Shutdown(World world)
    {
    }

    public void Update(float deltaSeconds)
    {
    }

    public void OnComponentCreated(IComponent component)
    {
        if (component is LightComponent asLightComponent)
            lock (_lights)
            {
                _lights.Add(asLightComponent);
            }
    }

    public void OnComponentDestroyed(IComponent component)
    {
        if (component is LightComponent asLightComponent)
            lock (_lights)
            {
                _lights.Remove(asLightComponent);
            }
    }

    public LightComponent[] GetLights()
    {
        lock (_lights)
        {
            return _lights.ToArray();
        }
    }
}