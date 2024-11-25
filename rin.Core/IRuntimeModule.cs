namespace rin.Core;

public interface IRuntimeModule
{
    public void Startup(SRuntime runtime);

    protected SRuntime? GetRuntime();

    public void Shutdown(SRuntime runtime);
    
}