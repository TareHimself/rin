namespace Rin.World.Ecs;

/// <summary>
///     64-bit FNV-1a, for hashes that must be the same in every process.
/// </summary>
internal static class Fnv
{
    public const ulong Basis = 14695981039346656037;

    private const ulong Prime = 1099511628211;

    public static ulong Mix(ulong hash, ulong value) => (hash ^ value) * Prime;

    public static ulong Mix(ulong hash, string text)
    {
        foreach (var c in text) hash = Mix(hash, c);
        return Mix(hash, (ulong)text.Length);
    }
}
