namespace Rin.World.Graphics.ImageBasedLighting;

/// <summary>
///     A handle to a map in an <see cref="EnvironmentMaps" />. <c>default</c> is no map, and a handle goes stale when
///     its map is freed.
/// </summary>
public readonly record struct EnvironmentMapHandle
{
    private readonly ulong _data;

    public EnvironmentMapHandle(uint index, uint generation)
    {
        _data = index | ((ulong)generation << 32);
    }

    internal uint Index => (uint)(_data & 0xFFFFFFFF);
    internal uint Generation => (uint)(_data >> 32);

    public bool IsValid => Generation != 0;
}
