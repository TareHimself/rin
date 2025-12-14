namespace Rin.Framework;

public interface ICopyable<out T> where T : class
{
    public T Copy();
}