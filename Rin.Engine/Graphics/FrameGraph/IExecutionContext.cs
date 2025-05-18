using Rin.Engine.Graphics.Descriptors;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.FrameGraph;

public interface IExecutionContext
{
    public DescriptorAllocator DescriptorAllocator { get; }
    public VkCommandBuffer GetCommandBuffer();
}