using JetBrains.Annotations;
using rin.Framework.Core.Extensions;
using rin.Framework.Scene.Components;

namespace rin.Framework.Scene.Entities;

public class Entity(Scene scene)
{
    [PublicAPI]
    public Scene Scene { get; } = scene;

    private readonly Dictionary<Type, List<IComponent>> _components = [];
    public string Id { get; set; } = string.Empty;

    [PublicAPI]
    public IComponent AddComponent(IComponent component)
    {
        var type = component.GetType();
        var attribute = (ComponentAttribute?)Attribute.GetCustomAttribute(type,typeof(ComponentAttribute));
        if(attribute == null) throw new Exception($"Component {type.Name} does not have a ComponentAttribute");
        foreach (var dependency in attribute.RequiredComponents)
        {
            var exists = false;
            lock (_components)
            {
                exists = _components.ContainsKey(dependency) && _components[dependency].NotEmpty();
            }

            if (!exists)
            {
                AddComponent(dependency);
            }
        }
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
        Scene.OnComponentAdded(component);
        return component;
    }
    
    [PublicAPI]
    public IComponent AddComponent(Type type)
    {
        var component = (IComponent?)Activator.CreateInstance(type,args: [this]);
        if (component == null)
        {
            throw new Exception("Failed to create component");
        }
        AddComponent(component);
        return component;
    }

    [PublicAPI]
    public T AddComponent<T>() where T : IComponent => (T)AddComponent(typeof(T));

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
}