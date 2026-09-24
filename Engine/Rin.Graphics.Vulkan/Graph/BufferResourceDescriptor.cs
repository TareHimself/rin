using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan.Graph;

public class BufferResourceDescriptor(ulong size, BufferCreateFlags usage) : IResourceDescriptor
{
    public readonly ulong Size = size;

    public readonly BufferCreateFlags Usage =
        BufferCreateFlags.Storage | BufferCreateFlags.DeviceAddress | usage;

    public override int GetHashCode()
    {
        return HashCode.Combine(Size, Usage);
    }
}
