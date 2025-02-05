namespace rin.Framework.Core.Extensions;

public static class NullableExtensions
{
    public static T GetValueOrEval<T>(this T? nullable, Func<T> evaluate) where T : unmanaged
    {
        if (nullable.HasValue)
        {
            return nullable.Value;
        }
        
        return evaluate();
    }
}