using JetBrains.Annotations;

namespace Rin.Core;

[NoReorder]
public readonly struct  Pair<T, Tu>(T inFirst, Tu inSecond)
{
    public readonly T First = inFirst;
    public readonly Tu Second = inSecond;

    public void Deconstruct(out T first, out Tu second)
    {
        first = First;
        second = Second;
    }
}