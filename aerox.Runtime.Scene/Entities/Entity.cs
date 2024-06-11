using aerox.Runtime.Extensions;
using aerox.Runtime.Math;
using aerox.Runtime.Scene.Components;
using aerox.Runtime.Scene.Graphics;

namespace aerox.Runtime.Scene.Entities;

public class Entity : SceneDisposable, ISceneDrawable
{
    private readonly Dictionary<Type, List<Component>> _components = new();
    private readonly List<Component> _componentsList = [];
    private readonly List<RenderedComponent> _renderedComponents = [];
    public SceneComponent? RootComponent;
    public Scene OwningScene = null!;


    public Entity? Parent => RootComponent?.Parent?.Owner;

    public void Collect(SceneFrame frame, Matrix4 parentSpace)
    {
        if (RootComponent == null) return;

        foreach (var comp in _renderedComponents)
            comp.Collect(frame,
                comp == RootComponent ? parentSpace : RootComponent.RelativeTransform * parentSpace);
    }

    protected virtual SceneComponent CreateRootComponent()
    {
        return AddComponent<SceneComponent>();
    }

    protected virtual void CreateDefaultComponents(SceneComponent root)
    {
    }

    public void Construct()
    {
        RootComponent = AddComponent(CreateRootComponent());
        CreateDefaultComponents(RootComponent);
    }

    public T AddComponent<T>(T component) where T : Component
    {
        var type = component.GetType();
        if (!_components.ContainsKey(type)) _components.Add(type, []);

        var list = _components[type];

        list.Add(component);
        _componentsList.Add(component);

        if (component is RenderedComponent renderedComponent) _renderedComponents.Add(renderedComponent);

        component.Owner = this;
        component.Start();

        return component;
    }

    public T AddComponent<T>() where T : Component
    {
        var comp = Activator.CreateInstance<T>();
        AddComponent(comp);
        return comp;
    }

    public T AddComponent<T>(Func<T> factory) where T : Component
    {
        var comp = factory();
        AddComponent(comp);
        return comp;
    }


    public T? FindComponent<T>() where T : Component
    {
        if (_components.TryGetValue(typeof(T), out var components))
            return (T?)components.FirstOrDefault();

        return null;
    }
    
    
    public T[] FindComponents<T>() where T : Component
    {
        if (_components.TryGetValue(typeof(T), out var components))
            return components.Select(a => (T)a).ToArray();

        return [];
    }


    protected override void OnDispose(bool isManual)
    {
        if (OwningScene.Drawer is { } drawer)
        {
            drawer.OnCollect -= Collect;
        }
        
        _renderedComponents.Clear();
        _components.Clear();
        foreach (var comp in _componentsList.AsReversed()) comp.Dispose();
        _componentsList.Clear();
        
        RootComponent?.Dispose();
        RootComponent = null;
    }

    protected override void OnTick(double deltaSeconds)
    {
        base.OnTick(deltaSeconds);
        foreach (var component in _componentsList) component.Tick(deltaSeconds);
    }


    protected override void OnStart()
    {
        base.OnStart();
        Construct();
        foreach (var component in _componentsList) component.Start();
        if (OwningScene.Drawer is { } drawer)
        {
            drawer.OnCollect += Collect;
        }
    }
}