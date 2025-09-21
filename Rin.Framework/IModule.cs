namespace Rin.Framework;

public interface IModule
{
    public void Start(IApplication app);
    public void Stop(IApplication app);
}