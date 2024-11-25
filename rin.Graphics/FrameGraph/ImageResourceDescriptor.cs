using TerraFX.Interop.Vulkan;

namespace rin.Graphics.FrameGraph;

public class ImageResourceDescriptor : IResourceDescriptor
{

    public required uint Width;
    public required uint Height;
    public required ImageFormat Format;
    public required VkImageUsageFlags Flags;
    public required VkImageLayout InitialLayout;
}