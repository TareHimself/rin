using System.Reflection;
using rin.Runtime.Core;
using rin.Runtime.Graphics;

namespace rin.Scene;

[NativeRuntimeModule(typeof(SGraphicsModule))]
public class SSceneModule : RuntimeModule, ISingletonGetter<SSceneModule>, ITickable
{
    private readonly Dictionary<string, Scene> _sceneMap = new();
    
    public static readonly string
        ShadersDir = Path.Join(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? "") ?? "","shaders","scene");

    public static SSceneModule Get()
    {
        return SRuntime.Get().GetModule<SSceneModule>();
    }

    public void Tick(double deltaSeconds)
    {
        foreach (var scene in _sceneMap) scene.Value.Tick(deltaSeconds);
    }

    public override void Startup(SRuntime runtime)
    {
        base.Startup(runtime);
        runtime.OnTick += Tick;
    }
    
    public T AddScene<T>(T scene) where T : Scene
    {
        _sceneMap.Add(scene.InstanceId, scene);
        scene.Start();
        return scene;
    }

    public T AddScene<T>(Func<T> factory) where T : Scene
    {
        return AddScene(factory());
    }

    public T AddScene<T>() where T : Scene
    {
        return AddScene(Activator.CreateInstance<T>());
    }
    
    public override void Shutdown(SRuntime runtime)
    {
        base.Shutdown(runtime);
        foreach (var (_, scene) in _sceneMap)
        {
            scene.Dispose();
        }
        
        _sceneMap.Clear();
    }
}