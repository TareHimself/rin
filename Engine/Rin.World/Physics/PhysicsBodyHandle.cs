namespace Rin.World.Physics;

public readonly record struct PhysicsBodyHandle
{
    private readonly ulong _data;

    public PhysicsBodyHandle(uint index, uint version)
    {
        _data = index | ((ulong)version << 32);
    }

    internal uint Index => (uint)(_data & 0xFFFFFFFF);
    internal uint Version => (uint)(_data >> 32);

    public static PhysicsBodyHandle Invalid => default;
    public bool IsValid => Version != 0;
}
