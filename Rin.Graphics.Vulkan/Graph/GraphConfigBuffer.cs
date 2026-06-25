using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan.Graph;

public class GraphConfigBuffer
{
    public required ulong Size { get; set; }
    public required VkBufferUsageFlags Usage { get; set; }

    public required bool Mapped { get; set; }
}