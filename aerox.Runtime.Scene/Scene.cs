using aerox.Runtime.Scene.Entities;
using aerox.Runtime.Scene.Graphics;
using aerox.Runtime.Scene.Graphics.Drawers;

namespace aerox.Runtime.Scene;

public class Scene : Disposable, ILifeCycle
{
    public readonly string InstanceId = Guid.NewGuid().ToString();
    
    private readonly Dictionary<string, Entity> _entityMap = new();

    public SceneDrawer? Drawer;

    protected SceneDrawer CreateDrawer() => new DeferredSceneDrawer();
    
    public void Start()
    {
        Drawer = CreateDrawer();
        Drawer.Start();
    }

    public T AddEntity<T>(T entity) where T : Entity
    {
        _entityMap.Add(entity.InstanceId,entity);
        return entity;
    }
    
    public T AddEntity<T>() where T : Entity => AddEntity(Activator.CreateInstance<T>());

    public T AddEntity<T>(Func<T> factory) where T : Entity => AddEntity(factory());
    protected override void OnDispose(bool isManual)
    {
        
    }

    public void Tick(double deltaSeconds)
    {
        foreach (var entity in _entityMap)
        {
            entity.Value.Tick(deltaSeconds);
        }
    }
}