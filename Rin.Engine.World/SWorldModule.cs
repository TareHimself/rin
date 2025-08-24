using Rin.Framework.Views;
using Rin.Sources;

namespace Rin.Engine.World;

[Module(typeof(SViewsModule))]
public class SWorldModule : IModule, ISingletonGetter<SWorldModule>
{
    public void Start(SEngine engine)
    {
        SEngine.Get().Sources.AddSource(AssemblyContentResource.New<SWorldModule>("World"));
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