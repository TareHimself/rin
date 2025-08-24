using Rin.Framework;

namespace Rin.Editor;

[Module]
public class SEditorModule : IModule, ISingletonGetter<SEditorModule>
{
    public void Start(SApplication application)
    {
        // throw new NotImplementedException();
    }

    public void Stop(SApplication application)
    {
        // throw new NotImplementedException();
    }


    public static SEditorModule Get()
    {
        return SApplication.Get().GetModule<SEditorModule>();
    }
}