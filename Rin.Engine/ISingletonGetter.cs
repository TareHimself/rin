namespace Rin.Engine;

public interface ISingletonGetter<out T>
{
    public static abstract T Get();
}