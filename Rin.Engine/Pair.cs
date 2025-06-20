namespace Rin.Engine;

public struct Pair<T, TU>(T inFirst, TU inSecond)
{
    public T First = inFirst;
    public TU Second = inSecond;

    public void Deconstruct(out T first, out TU second)
    {
        first = First;
        second = Second;
    }
}