namespace Rin.Engine.Core;

public interface IModule
{
    public void Start(SEngine engine);

    public void Stop(SEngine engine);
}