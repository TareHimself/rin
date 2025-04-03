namespace Rin.Engine;

public interface ICloneable<out T>
{
    public T Clone();
}