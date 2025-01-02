using rin.Framework.Scene.Components;
using rin.Framework.Scene.Entities;
using rin.Framework.Scene.Systems;

namespace rin.Framework.Scene;

public class Scene
{
    private readonly List<ISystem> _systemsList = [];
    private readonly Dictionary<Type, ISystem> _systems = [];
    private readonly Dictionary<string, Entity> _entities = [];
    private readonly Dictionary<string, IComponent> _components = [];
    private object _lock = new();

    
    public Entity AddEntity(Entity entity)
    {
        throw new Exception();
    }

    public ISystem AddSystem(ISystem system)
    {
        _systems.Add(system.GetType(), system);
        return system;
    }

    public void OnComponentAdded(IComponent component)
    {
        component.Id = Guid.NewGuid().ToString();
        _components.Add(component.Id, component);
        foreach (var system in _systemsList)
        {
            system.OnComponentCreated(component);
        }
    }
    
    public void OnComponentRemoved(IComponent component)
    {
        _components.Remove(component.Id);
        foreach (var system in _systemsList)
        {
            system.OnComponentDestroyed(component);
        }
    }

    public Entity CreateEntity()
    {
        var id = Guid.NewGuid().ToString();
        var entity = new Entity(this)
        {
            Id = id,
        };
        
        lock (_entities)
        {
            _entities.Add(id, entity);
        }

        return entity;
    }
}