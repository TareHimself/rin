namespace Rin.World.Graphics.Mesh;

/// <summary>
///     A handle to a mesh in an <see cref="IMeshFactory" />. <c>default</c> is no mesh, and a handle goes stale when its
///     mesh is freed.
/// </summary>
public readonly record struct MeshHandle
{
    private readonly ulong _data;

    public MeshHandle(uint index, uint generation)
    {
        _data = index | ((ulong)generation << 32);
    }

    internal uint Index => (uint)(_data & 0xFFFFFFFF);
    internal uint Generation => (uint)(_data >> 32);

    public bool IsValid => Generation != 0;
}
