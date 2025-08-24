namespace Rin.Framework;

public interface ISingletonGetter<out T>
{
    public static abstract T Get();
}