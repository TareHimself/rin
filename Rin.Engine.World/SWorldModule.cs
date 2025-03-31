using Rin.Engine.Core;
using Rin.Engine.Views;
using Rin.Sources;

namespace Rin.Engine.World;

[Module(typeof(SViewsModule))]
public class SWorldModule : IModule, ISingletonGetter<SWorldModule>
{
    public void Start(SEngine engine)
    {
        SEngine.Get().Sources.AddSource(
            new ResourcesSource(typeof(SWorldModule).Assembly, "World", ".Content.World."));
    }

    public void Stop(SEngine engine)
    {
        //throw new NotImplementedException();
    }
    
    public static SWorldModule Get()
    {
        return SEngine.Get().GetModule<SWorldModule>();
    }
}