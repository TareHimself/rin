using JetBrains.Annotations;
using rin.Framework.Core;
using rin.Framework.Scene.Components;
using rin.Framework.Scene.Components.Lights;
using rin.Framework.Scene.Entities;
using rin.Framework.Scene.Systems;

namespace rin.Framework.Scene;

public class Scene : ITickable
{
    private readonly List<ISystem> _tickableSystems = [];
    private readonly Dictionary<Type, ISystem> _systems = [];
    private readonly Dictionary<string, Entity> _entities = [];
    
    
    [PublicAPI]
    public Entity AddEntity(Entity entity)
    {
        lock (_entities)
        {
            _entities.Add(entity.Id, entity);
            entity.Scene = this;
        }
        entity.Init();
        return entity;
    }
    
    [PublicAPI]
    public T AddEntity<T>() where T : Entity
    {
        var entity = Activator.CreateInstance<T>();
        AddEntity(entity);
        return entity;
    }

    [PublicAPI]
    public ISystem AddSystem(ISystem system)
    {
        _systems.Add(system.GetType(), system);
        if (system.Tickable)
        {
            _tickableSystems.Add(system);
        }
        return system;
    }

    public void Tick(double delta)
    {
        foreach (var tickableSystem in _tickableSystems)
        {
            tickableSystem.Tick(delta);
        }
    }
    
    [PublicAPI]
    public IEnumerable<SceneComponent> GetRoots()
    {
        lock (_entities)
        {
            foreach (var (key, value) in _entities)
            {
                if (value.RootComponent is { } component)
                {
                    yield return component;
                }
            }
        }
    }

    [PublicAPI]
    public IEnumerable<SceneComponent> GetPureRoots()
    {
        foreach (var root in GetRoots())
        {
            if (root is { Parent: null } component)
            {
                yield return component;
            }
        }
    }
}