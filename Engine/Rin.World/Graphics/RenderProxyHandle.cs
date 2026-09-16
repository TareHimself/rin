namespace Rin.World.Graphics;

public readonly record struct RenderProxyHandle
{
    private readonly ulong _data;

    public RenderProxyHandle(uint index, uint version)
    {
        _data = index | ((ulong)version << 32);
    }

    internal uint Index => (uint)(_data & 0xFFFFFFFF);
    internal uint Version => (uint)(_data >> 32);

    public static RenderProxyHandle Invalid => default;
    public bool IsValid => Version != 0;
}
