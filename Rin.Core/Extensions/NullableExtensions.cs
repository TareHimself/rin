namespace Rin.Core.Extensions;

public static class NullableExtensions
{
    public static T GetValueOrEval<T>(this T? nullable, Func<T> evaluate) where T : unmanaged
    {
        return nullable ?? evaluate();
    }
}