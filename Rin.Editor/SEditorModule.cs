using Rin.Framework;

namespace Rin.Editor;

[Module]
public class SEditorModule : IModule, ISingletonGetter<SEditorModule>
{
    public void Start(IApplication app)
    {
        // throw new NotImplementedException();
    }

    public void Stop(IApplication app)
    {
        // throw new NotImplementedException();
    }


    public static SEditorModule Get()
    {
        return SFramework.Get().GetModule<SEditorModule>();
    }
}