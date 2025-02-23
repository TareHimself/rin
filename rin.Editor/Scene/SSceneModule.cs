using rin.Framework.Core;
using rin.Framework.Views;

namespace rin.Editor.Scene;

[Module(typeof(SViewsModule))]
public class SSceneModule : IModule, ISingletonGetter<SSceneModule>
{
    public void Start(SRuntime runtime)
    {
        //throw new NotImplementedException();
    }

    public void Stop(SRuntime runtime)
    {
        //throw new NotImplementedException();
    }

    public Scene CreateScene()
    {
        return new Scene();
    }

    public static SSceneModule Get()
    {
        return SRuntime.Get().GetModule<SSceneModule>();
    }
}