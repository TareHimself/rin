using Rin.Engine.Graphics.Descriptors;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.FrameGraph;

public interface IExecutionContext
{
    public VkCommandBuffer GetCommandBuffer();

    public DescriptorAllocator DescriptorAllocator { get; }
}