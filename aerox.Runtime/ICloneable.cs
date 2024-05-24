namespace aerox.Runtime;

public interface ICloneable<out T>
{
    public T Clone();
}