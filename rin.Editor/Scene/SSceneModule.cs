using Rin.Engine.Core;
using Rin.Engine.Views;

namespace Rin.Editor.Scene;

[Module(typeof(SViewsModule))]
public class SSceneModule : IModule, ISingletonGetter<SSceneModule>
{
    public void Start(SEngine engine)
    {
        //throw new NotImplementedException();
    }

    public void Stop(SEngine engine)
    {
        //throw new NotImplementedException();
    }

    public Scene CreateScene()
    {
        return new Scene();
    }

    public static SSceneModule Get()
    {
        return SEngine.Get().GetModule<SSceneModule>();
    }
}