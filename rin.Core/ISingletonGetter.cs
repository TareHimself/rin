namespace rin.Core;

public interface ISingletonGetter<out T>
{
    public static abstract T Get();
}