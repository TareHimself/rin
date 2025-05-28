using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.FrameGraph;

public class BufferResourceDescriptor(ulong size,in VkBufferUsageFlags usage,bool mapped) : IResourceDescriptor
{
    public readonly ulong Size = size;
    public readonly VkBufferUsageFlags Usage = VkBufferUsageFlags.VK_BUFFER_USAGE_STORAGE_BUFFER_BIT |
                                               VkBufferUsageFlags.VK_BUFFER_USAGE_SHADER_DEVICE_ADDRESS_BIT | usage;

    public readonly bool Mapped = mapped;

    public override int GetHashCode()
    {
        return HashCode.Combine(Size, Usage,Mapped);
    }
}