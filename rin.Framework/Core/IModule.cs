namespace rin.Framework.Core;

public interface IModule
{
    public void Start(SRuntime runtime);

    public void Stop(SRuntime runtime);
}