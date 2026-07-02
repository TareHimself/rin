using Rin.Core;

namespace Rin.Editor;

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
        return Global.Get().GetModule<SEditorModule>();
    }
}