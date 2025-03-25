using Rin.Engine.Core;
using Rin.Engine.Views;
using Rin.Sources;

namespace Rin.Engine.Scene;

[Module(typeof(SViewsModule))]
public class SSceneModule : IModule, ISingletonGetter<SSceneModule>
{
    public void Start(SEngine engine)
    {
        SEngine.Get().Sources.AddSource(
            new ResourcesSource(typeof(SSceneModule).Assembly, "Scene", ".Content.Scene."));
    }

    public void Stop(SEngine engine)
    {
        //throw new NotImplementedException();
    }
    
    public static SSceneModule Get()
    {
        return SEngine.Get().GetModule<SSceneModule>();
    }
}