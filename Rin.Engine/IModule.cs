namespace Rin.Engine;

public interface IModule
{
    public void Start(SEngine engine);

    public void Stop(SEngine engine);
}