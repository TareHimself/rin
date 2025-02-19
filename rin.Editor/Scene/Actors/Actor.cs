using System.Numerics;
using JetBrains.Annotations;
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Editor.Scene.Components;
namespace rin.Editor.Scene.Actors;

public class Actor : IReceivesUpdate
{
    private SceneComponent? _root;
    
    public bool Active { get; protected set; }
    [PublicAPI] public Scene? Scene { get; set; }

    private readonly Dictionary<Type, List<IComponent>> _components = [];
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The Root of this entity, if null this entity cannot be transformed or drawn in the scene
    /// </summary>
    public SceneComponent? RootComponent
    {
        get => _root;
        set
        {
            if (_root != null)
            {
                RemoveComponent(_root);
            }

            if (value != null)
            {
                AddComponent(value);
            }

            _root = value;
        }
    }

    [PublicAPI]
    public T AddComponent<T>(T component) where T : IComponent
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
                _components.Add(component.GetType(), [component]);
            }
        }

        component.Owner = this;
        if (Active)
        {
            component.Start();
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

    [PublicAPI]
    public void RemoveComponent(IComponent component)
    {
        lock (_components)
        {
            var type = component.GetType();

            if (_components.TryGetValue(type, out var components))
            {
                components.Remove(component);
            }
        }
    }

    public T? FindComponent<T>() where T : IComponent
    {
        lock (_components)
        {
            var type = typeof(T);
            if (_components.TryGetValue(type, out var components))
            {
                return (T?)components.FirstOrDefault();
            }
        }

        return default;
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
            foreach (var (_, comps) in _components)
            {
                foreach (var component in comps)
                {
                    yield return component;
                }
            }
        }
    }

    [PublicAPI]
    public virtual void Start()
    {
        if(Active) return;
        Active = true;
        foreach (var component in GetComponents().ToArray())
        {
            component.Start();
        }
    }

    [PublicAPI]
    public virtual void Stop()
    {
        if(!Active) return;
        Active = false;
        foreach (var component in GetComponents().ToArray())
        {
            component.Stop();
        }

        lock (_components)
        {
            _components.Clear();
        }
    }

    public void Update(double deltaSeconds)
    {
        if(!Active) return;
        
        foreach (var component in GetComponents().ToArray())
        {
            component.Update(deltaSeconds);
        }
    }

    public bool AttachTo(SceneComponent target)
    {
        return _root?.AttachTo(target) ?? false;
    }
    
    public bool AttachTo(Actor target)
    {
        if (target.RootComponent is { } component)
        {
            return _root?.AttachTo(component) ?? false;
        }

        return false;
    }

    public bool Detach()
    {
        return _root?.Detach() ?? false;
    }

    public Transform GetRelativeTransform()
    {
        return _root?.GetRelativeTransform() ?? new Transform();
    }

    public Vector3 GetRelativeLocation()
    {
        return _root?.GetRelativeLocation() ?? new Vector3();
    }

    public Rotator GetRelativeRotation()
    {
        return _root?.GetRelativeRotation() ?? new Rotator();
    }

    public Vector3 GetRelativeScale()
    {
        return _root?.GetRelativeScale() ?? new Vector3();
    }

    public void SetRelativeTransform(Transform transform)
    {
        _root?.SetRelativeTransform(transform);
    }

    public void SetRelativeLocation(Vector3 location)
    {
        _root?.SetRelativeLocation(location);
    }

    public void SetRelativeRotation(Rotator rotation)
    {
        _root?.SetRelativeRotation(rotation);
    }

    public void SetRelativeScale(Vector3 scale)
    {
        _root?.SetRelativeScale(scale);
    }
    
    [PublicAPI]
    public void AddRelativeLocation( float? x = null, float? y = null,
        float? z = null)
    {
        _root?.AddRelativeLocation(x,y,z);
    }
    [PublicAPI]
    public void AddRelativeRotation(float? pitch = null, float? yaw = null, float? roll = null)
    {
        _root?.AddRelativeRotation(pitch, yaw, roll);
    }
    [PublicAPI]
    public void AddRelativeScale(float? x = null, float? y = null, float? z = null)
    {
        _root?.AddRelativeScale(x,y,z);
    }

    public Transform GetWorldTransform()
    {
       return _root?.GetSceneTransform() ?? new Transform();
    }

    public void SetWorldTransform(Transform worldTransform)
    {
        _root?.SetSceneTransform(worldTransform);
    }
}