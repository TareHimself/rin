namespace aerox.Runtime;

public interface ISingletonGetter<out T>
{
    public static abstract T Get();
}