using aerox.Runtime.Graphics;

namespace aerox.Runtime.Scene;

[NativeRuntimeModule(typeof(GraphicsModule))]
public class SceneModule : RuntimeModule,ISingletonGetter<SceneModule>, ITickable
{

    private readonly Dictionary<string,Scene> _sceneMap = new();

    public override void Startup(Runtime runtime)
    {
        base.Startup(runtime);
        runtime.OnTick += Tick;
    }

    public T AddScene<T>(Func<T> factory) where T : Scene
    {
        var newScene = factory();
        _sceneMap.Add(newScene.InstanceId,newScene);
        return newScene;
    }
    
    public T AddScene<T>() where T : Scene
    {
        var newScene = Activator.CreateInstance<T>();
        _sceneMap.Add(newScene.InstanceId,newScene);
        return newScene;
    }

    public void Tick(double deltaSeconds)
    {
        foreach (var scene in _sceneMap)
        {
            scene.Value.Tick(deltaSeconds);
        }
    }

    public static SceneModule Get()
    {
        return Runtime.Instance.GetModule<SceneModule>();
    }
}