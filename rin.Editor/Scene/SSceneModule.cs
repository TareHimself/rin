using Rin.Engine.Core;
using Rin.Engine.Views;
using Rin.Sources;

namespace Rin.Editor.Scene;

[Module(typeof(SViewsModule))]
public class SSceneModule : IModule, ISingletonGetter<SSceneModule>
{
    public void Start(SEngine engine)
    {
        SEngine.Get().Sources.AddSource(
            new ResourcesSource(typeof(SSceneModule).Assembly, "Editor", ".Content.Editor."));
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