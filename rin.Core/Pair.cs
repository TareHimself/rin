namespace rin.Core;

public class Pair<T, TU>(T inFirst, TU inSecond)
{
    public readonly T First = inFirst;
    public readonly TU Second = inSecond;

    public void Deconstruct(out T first, out TU second)
    {
        first = First;
        second = Second;
    }
}