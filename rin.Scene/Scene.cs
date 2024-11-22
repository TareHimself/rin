using rin.Core;
using rin.Scene.Components;
using rin.Scene.Components.Lights;
using rin.Scene.Entities;
using rin.Scene.Graphics;

namespace rin.Scene;

public class Scene : Disposable, ILifeCycle
{
    private readonly Dictionary<string, Entity> _entityMap = new();
    public readonly string InstanceId = Guid.NewGuid().ToString();

    public SceneDrawer? Drawer { get; private set;}

    public readonly HashSet<LightComponent> Lights = [];
    
    public Entity? ViewTarget { get; private set; }
    
    public bool Active { get; private set; }
    
    public void Tick(double deltaSeconds)
    {
        foreach (var (_, entity) in _entityMap) entity.Tick(deltaSeconds);
    }

    protected virtual SceneDrawer CreateDrawer() => new SceneDrawer(this);
    
    public void SetViewTarget(Entity target)
    {
        ViewTarget = target;
    }
    
    public void Start()
    {
        Drawer = CreateDrawer();
        Drawer.Start();
        
        foreach (var (_, entity) in _entityMap) entity.Start();
        
        Active = true;
    }

    public T AddEntity<T>(T entity) where T : Entity
    {
        _entityMap.Add(entity.InstanceId, entity);
        entity.OwningScene = this;
        
        if (Active) entity.Start();
        
        return entity;
    }

    public T AddEntity<T>() where T : Entity
    {
        return AddEntity(Activator.CreateInstance<T>());
    }

    public T AddEntity<T>(Func<T> factory) where T : Entity
    {
        return AddEntity(factory());
    }

    protected override void OnDispose(bool isManual)
    {
        Active = false;
        Drawer?.Dispose();
        
        foreach (var (_, entity) in _entityMap)
        {
            entity.Dispose();
        }
        
        _entityMap.Clear();
    }
}