namespace rin.Framework.Core;

public interface ITickable
{
    void Tick(double deltaSeconds);
}