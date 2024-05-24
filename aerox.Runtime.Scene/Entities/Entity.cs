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


    public Entity? Parent
    {
        get { return RootComponent?.Parent?.Owner; }
    }

    protected virtual SceneComponent CreateRootComponent() => AddComponent<SceneComponent>();

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
        var type = typeof(T);
        if (!_components.ContainsKey(type)) _components.Add(type, []);
        
        var list = _components[type];

        list.Add(component);
        _componentsList.Add(component);

        if (component is RenderedComponent renderedComponent)
        {
            _renderedComponents.Add(renderedComponent);
        }

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
            return components.Count == 0 ? null : (T)components.Last();

        return null;
    }


    protected override void OnDispose(bool isManual)
    {
        _renderedComponents.Clear();
        _components.Clear();
        foreach (var comp in _componentsList.AsReversed())
        {
            comp.Dispose();
        }
        _componentsList.Clear();
    }

    protected override void OnTick(double deltaSeconds)
    {
        base.OnTick(deltaSeconds);
        foreach (var component in _componentsList)
        {
            component.Tick(deltaSeconds);
        }
    }


    protected override void OnStart()
    {
        base.OnStart();
        foreach (var component in _componentsList)
        {
            component.Start();
        }
    }

    public void Draw(SceneFrame frame, Matrix4 parentSpace)
    {
        if(RootComponent == null) return;
        
        foreach (var comp in _renderedComponents)
        {
            comp.Draw(frame,
                comp == RootComponent ? parentSpace : RootComponent.RelativeTransform.RelativeTo(parentSpace));
        }
    }
}