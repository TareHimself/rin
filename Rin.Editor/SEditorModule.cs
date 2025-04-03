using Rin.Engine;

namespace Rin.Editor;

[Module]
public class SEditorModule : IModule, ISingletonGetter<SEditorModule>
{
    public void Start(SEngine engine)
    {
        // throw new NotImplementedException();
    }

    public void Stop(SEngine engine)
    {
        // throw new NotImplementedException();
    }


    public static SEditorModule Get()
    {
        return SEngine.Get().GetModule<SEditorModule>();
    }
}