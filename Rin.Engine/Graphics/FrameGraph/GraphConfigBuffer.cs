using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.FrameGraph;

public class GraphConfigBuffer
{
    public required ulong Size { get; set; }
    public required VkBufferUsageFlags Usage { get; set; }

    public required bool Mapped { get; set; }
}