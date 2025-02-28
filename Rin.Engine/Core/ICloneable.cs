namespace Rin.Engine.Core;

public interface ICloneable<out T>
{
    public T Clone();
}