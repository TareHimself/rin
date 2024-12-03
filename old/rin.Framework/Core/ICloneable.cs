namespace rin.Framework.Core;

public interface ICloneable<out T>
{
    public T Clone();
}