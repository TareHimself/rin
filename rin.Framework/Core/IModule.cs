namespace rin.Framework.Core;

public interface IModule
{
    public void Startup(SRuntime runtime);

    public void Shutdown(SRuntime runtime);
}