namespace Rin.Framework;

public interface IModule
{
    public void Start(SApplication application);

    public void Stop(SApplication application);
}