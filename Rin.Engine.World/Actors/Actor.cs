using System.Diagnostics;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.World.Components;
using Rin.Engine.World.Math;
using Rin.Framework.Shared.Math;

namespace Rin.Engine.World.Actors;

public class Actor : IReceivesUpdate
{
    private readonly Dictionary<Type, List<IComponent>> _components = [];
    private WorldComponent? _root;

    public bool Active { get; private set; }
    [PublicAPI] public World? World { get; set; }
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    ///     The Root of this entity, if null this entity cannot be transformed or drawn in the scene
    /// </summary>
    public WorldComponent? RootComponent
    {
        get => _root;
        set
        {
            if (value != null)
                if (!HasComponent(value))
                    AddComponent(value);
            _root = value;
        }
    }

    public WorldComponent[] InitialComponents
    {
        init
        {
            foreach (var component in value) AddComponent(component);
        }
    }

    public void Update(float deltaSeconds)
    {
        if (!Active) return;

        foreach (var component in GetComponents().ToArray()) component.Update(deltaSeconds);
    }

    [PublicAPI]
    public T AddComponent<T>(T component) where T : IComponent
    {
        if (_components.ContainsKey(component.GetType()))
            _components[component.GetType()].Add(component);
        else
            _components.Add(component.GetType(), [component]);
        component.Owner = this;
        if (Active)
        {
            Debug.Assert(World != null);
            Debug.Assert(RootComponent != null);
            if (!ReferenceEquals(RootComponent, component) && component is IWorldComponent asSceneComponent)
                asSceneComponent.AttachTo(RootComponent);
            if (component is IPhysicsComponent asPhysicsComponent) World.AddPhysicsComponent(asPhysicsComponent);
            component.Start();
        }

        return component;
    }

    [PublicAPI]
    public IComponent AddComponent(Type type)
    {
        var component = (IComponent?)Activator.CreateInstance(type);
        if (component == null) throw new Exception("Failed to create component");

        AddComponent(component);
        return component;
    }

    [PublicAPI]
    public T AddComponent<T>() where T : IComponent
    {
        var component = Activator.CreateInstance<T>();
        if (component == null) throw new Exception("Failed to create component");

        AddComponent(component);
        return component;
    }

    [PublicAPI]
    public void RemoveComponent(IComponent component)
    {
        var type = component.GetType();

        if (_components.TryGetValue(type, out var components))
        {
            components.Remove(component);
            if (Active)
            {
                Debug.Assert(World != null);
                component.Stop();
                if (component is IPhysicsComponent asPhysicsComponent) World.RemovePhysicsComponent(asPhysicsComponent);
            }
        }
    }

    public bool HasComponent<T>(T component) where T : IComponent
    {
        var type = typeof(T);
        return _components.TryGetValue(type, out var components) && components.Contains(component);
    }

    public bool HasComponentByType<T>() where T : IComponent
    {
        var type = typeof(T);
        return _components.ContainsKey(type);
    }

    public T? FindComponentByType<T>() where T : IComponent
    {
        var type = typeof(T);
        if (_components.TryGetValue(type, out var components)) return (T?)components.FirstOrDefault();
        return default;
    }

    public IComponent[] FindComponentsByType<T>() where T : IComponent
    {
        var type = typeof(T);
        if (_components.TryGetValue(type, out var components)) return components.ToArray();
        return [];
    }

    [PublicAPI]
    public IEnumerable<IComponent> GetComponents()
    {
        return _components.Values.SelectMany(c => c);
    }

    [PublicAPI]
    public virtual void Start()
    {
        if (Active) return;
        Active = true;
        Debug.Assert(World != null);
        Debug.Assert(RootComponent != null);
        var comps = GetComponents().ToArray();
        foreach (var component in comps)
        {
            if (component != RootComponent && component is IWorldComponent asSceneComponent)
                asSceneComponent.AttachTo(RootComponent);
            if (component is IPhysicsComponent asPhysicsComponent) World.AddPhysicsComponent(asPhysicsComponent);
            component.Start();
        }
    }

    [PublicAPI]
    public virtual void Stop()
    {
        if (!Active) return;
        Active = false;
        Debug.Assert(World != null);
        foreach (var component in GetComponents().ToArray())
        {
            if (component is IPhysicsComponent asPhysicsComponent) World.AddPhysicsComponent(asPhysicsComponent);
            component.Stop();
        }

        _components.Clear();
    }

    public bool AttachTo(WorldComponent target)
    {
        return _root?.AttachTo(target) ?? false;
    }

    public bool AttachTo(Actor target)
    {
        if (target.RootComponent is { } component) return _root?.AttachTo(component) ?? false;

        return false;
    }

    public bool Detach()
    {
        return _root?.Detach() ?? false;
    }

    public void SetLocation(in Vector3 location, Space space = Space.Local)
    {
        _root?.SetLocation(location, space);
    }

    public void Translate(in Vector3 translation, Space space = Space.Local)
    {
        _root?.Translate(translation, space);
    }

    public void SetRotation(in Quaternion rotation, Space space = Space.Local)
    {
        _root?.SetRotation(rotation, space);
    }

    public void Rotate(in Vector3 axis, float delta, Space space = Space.Local)
    {
        _root?.Rotate(axis, delta, space);
    }

    public void SetScale(in Vector3 scale, Space space = Space.Local)
    {
        _root?.SetScale(scale, space);
    }

    public void SetTransform(in Transform transform, Space space = Space.Local)
    {
        _root?.SetTransform(transform, space);
    }

    public Vector3 GetLocation(Space space = Space.Local)
    {
        return _root?.GetLocation(space) ?? new Vector3();
    }

    public Quaternion GetRotation(Space space = Space.Local)
    {
        return _root?.GetRotation(space) ?? new Quaternion();
    }

    public Vector3 GetScale(Space space = Space.Local)
    {
        return _root?.GetScale(space) ?? new Vector3();
    }

    public Transform GetTransform(Space space = Space.Local)
    {
        return _root?.GetTransform(space) ?? new Transform();
    }
}