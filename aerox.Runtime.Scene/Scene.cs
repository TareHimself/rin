using aerox.Runtime.Scene.Entities;
using aerox.Runtime.Scene.Graphics;

namespace aerox.Runtime.Scene;

public class Scene : Disposable, ILifeCycle
{
    private readonly Dictionary<string, Entity> _entityMap = new();
    public readonly string InstanceId = Guid.NewGuid().ToString();

    public SceneDrawer? Drawer;

    public void Tick(double deltaSeconds)
    {
        foreach (var entity in _entityMap) entity.Value.Tick(deltaSeconds);
    }

    protected SceneDrawer CreateDrawer()
    {
        return new DeferredSceneDrawer();
    }

    public void Start()
    {
        Drawer = CreateDrawer();
        Drawer.Start();
    }

    public T AddEntity<T>(T entity) where T : Entity
    {
        _entityMap.Add(entity.InstanceId, entity);
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
    }
}