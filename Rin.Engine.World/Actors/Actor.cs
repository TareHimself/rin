using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Core;
using Rin.Engine.World.Components;
using Rin.Engine.World.Math;

namespace Rin.Engine.World.Actors;

public class Actor : IReceivesUpdate
{
    private readonly Dictionary<Type, List<IComponent>> _components = [];
    private SceneComponent? _root;

    public bool Active { get; private set; }
    [PublicAPI] public World? World { get; set; }
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    ///     The Root of this entity, if null this entity cannot be transformed or drawn in the scene
    /// </summary>
    public SceneComponent? RootComponent
    {
        get => _root;
        set
        {
            if (_root != null) RemoveComponent(_root);

            if (value != null) AddComponent(value);

            _root = value;
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
        if (Active) component.Start();

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

        if (_components.TryGetValue(type, out var components)) components.Remove(component);
    }

    public T? FindComponent<T>() where T : IComponent
    {
        lock (_components)
        {
            var type = typeof(T);
            if (_components.TryGetValue(type, out var components)) return (T?)components.FirstOrDefault();
        }

        return default;
    }

    public IComponent[] FindComponents<T>() where T : IComponent
    {
        var type = typeof(T);
        if (_components.TryGetValue(type, out var components)) return components.ToArray();

        return [];
    }

    [PublicAPI]
    public IEnumerable<IComponent> GetComponents()
    {
        foreach (var (_, comps) in _components)
        foreach (var component in comps)
            yield return component;
    }

    [PublicAPI]
    public virtual void Start()
    {
        if (Active) return;
        Active = true;
        foreach (var component in GetComponents().ToArray()) component.Start();
    }

    [PublicAPI]
    public virtual void Stop()
    {
        if (!Active) return;
        Active = false;
        foreach (var component in GetComponents().ToArray()) component.Stop();

        _components.Clear();
    }

    public bool AttachTo(SceneComponent target)
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