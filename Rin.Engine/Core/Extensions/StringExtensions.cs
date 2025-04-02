namespace Rin.Engine.Core.Extensions;

public static class StringExtensions
{
    public static bool ContainsAll(this string source, params char[] data)
    {
        foreach (var c in data)
            if (!source.Contains(c))
                return false;

        return true;
    }

    public static bool ContainsAll(this string source, params string[] data)
    {
        foreach (var c in data)
            if (!source.Contains(c))
                return false;

        return true;
    }
}