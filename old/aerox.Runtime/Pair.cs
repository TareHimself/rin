namespace aerox.Runtime;

public class Pair<T, TU>(T inFirst, TU inSecond)
{
    public readonly T First = inFirst;
    public readonly TU Second = inSecond;
}