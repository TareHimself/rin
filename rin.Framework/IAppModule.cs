using rin.Framework.Core;

namespace rin.Framework;

public interface IAppModule
{

    public App GetApp();
    
    public void Start(App app);

    public void Stop(App app);
    
}