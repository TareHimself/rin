namespace Rin.Core.Extensions;

public static class StringExtensions
{
    extension(string source)
    {
        public bool ContainsAll(params char[] data)
        {
            foreach (var c in data)
                if (!source.Contains(c))
                    return false;

            return true;
        }

        public bool ContainsAll(params string[] data)
        {
            foreach (var c in data)
                if (!source.Contains(c))
                    return false;

            return true;
        }
    }
}