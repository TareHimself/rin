using JetBrains.Annotations;
using rin.Framework.Core;
using rin.Framework.Core.Extensions;
using rin.Framework.Scene.Components;

namespace rin.Framework.Scene.Entities;

public class Entity : ITickable
{

    private SceneComponent? _root;
    
    [PublicAPI]
    public Scene? Scene { get; set; }

    private readonly Dictionary<Type, List<IComponent>> _components = [];
    public string Id { get; set; } = Guid.NewGuid().ToString();

    private bool _hasInit = false;
    
    /// <summary>
    /// The Root of this entity, if null this entity cannot be transformed or drawn in the scene
    /// </summary>
    public SceneComponent? RootComponent
    {
        get => _root;
        set => _root = value;
    }

    [PublicAPI]
    public IComponent AddComponent(IComponent component)
    {
        //var type = component.GetType();
        // var attribute = (ComponentAttribute?)Attribute.GetCustomAttribute(type,typeof(ComponentAttribute));
        // if(attribute == null) throw new Exception($"Component {type.Name} does not have a ComponentAttribute");
        // foreach (var dependency in attribute.RequiredComponents)
        // {
        //     var exists = false;
        //     lock (_components)
        //     {
        //         exists = _components.ContainsKey(dependency) && _components[dependency].NotEmpty();
        //     }
        //
        //     if (!exists)
        //     {
        //         AddComponent(dependency);
        //     }
        // }
        lock (_components)
        {
            
            if (_components.ContainsKey(component.GetType()))
            {
                _components[component.GetType()].Add(component);
            }
            else
            {
                _components.Add(component.GetType(),[component]);
            }
        }
        component.Owner = this;
        if (_hasInit)
        {
            component.Init();
        }
        return component;
    }
    
    [PublicAPI]
    public IComponent AddComponent(Type type)
    {
        var component = (IComponent?)Activator.CreateInstance(type);
        if (component == null)
        {
            throw new Exception("Failed to create component");
        }
        AddComponent(component);
        return component;
    }

    [PublicAPI]
    public T AddComponent<T>() where T : IComponent
    {
        var component = Activator.CreateInstance<T>();
        if (component == null)
        {
            throw new Exception("Failed to create component");
        }
        AddComponent(component);
        return component;
    }

    public IComponent? FindComponent<T>() where T : IComponent
    {
        lock (_components)
        {
            var type = typeof(T);
            if (_components.TryGetValue(type, out var components))
            {
                return components.FirstOrDefault();
            }
        }

        return null;
    }
    
    public IComponent[] FindComponents<T>() where T : IComponent
    {
        lock (_components)
        {
            var type = typeof(T);
            if (_components.TryGetValue(type, out var components))
            {
                return components.ToArray();
            }
        }

        return [];
    }
    
    [PublicAPI]
    public IEnumerable<IComponent> GetComponents()
    {
        lock (_components)
        {
            foreach (var (_,comps) in _components)
            {
                foreach (var component in comps)
                {
                    yield return component;
                }
            }
        }
    }

    [PublicAPI]
    public virtual void Init()
    {
        _hasInit = true;
        foreach (var component in GetComponents().ToArray())
        {
            component.Init();
        }
    }

    [PublicAPI]
    public virtual void Destroy()
    {
        if (_hasInit)
        {
            foreach (var component in GetComponents().ToArray())
            {
                component.Destroy();
            }

            lock (_components)
            {
                _components.Clear();
            }
        }
    }

    public void Tick(double delta)
    {
        foreach (var component in GetComponents().ToArray())
        {
            component.Tick(delta);
        }
    }
}