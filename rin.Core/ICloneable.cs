namespace rin.Core;

public interface ICloneable<out T>
{
    public T Clone();
}