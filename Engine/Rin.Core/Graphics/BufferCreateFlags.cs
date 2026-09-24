namespace Rin.Core.Graphics;

[Flags]
public enum BufferCreateFlags
{
    None = 0,

    /// <summary>The host reads from this buffer (e.g. a readback/staging-for-readback buffer).</summary>
    HostSrc = 1 << 0,

    /// <summary>The host writes to this buffer (e.g. a staging/upload buffer, or a directly mapped one).</summary>
    HostDst = 1 << 1,
    TransferSrc = 1 << 2,
    TransferDst = 1 << 3,
    Storage = 1 << 4,
    Uniform = 1 << 5,
    Index = 1 << 6,
    Indirect = 1 << 7,
    DeviceAddress = 1 << 8,
}