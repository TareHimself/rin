namespace Rin.World.Ecs;

/// <summary>
///     A process-wide, append-only number per component struct, which each <see cref="ComponentSchema" /> maps to its own
///     <see cref="ComponentId" />.
/// </summary>
internal static class TypeIndex
{
    private static int _count;

    public static int Next() => Interlocked.Increment(ref _count) - 1;
}

internal static class TypeIndex<T> where T : unmanaged
{
    public static readonly int Value = TypeIndex.Next();
}
