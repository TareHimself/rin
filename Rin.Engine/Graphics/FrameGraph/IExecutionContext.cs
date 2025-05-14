using Rin.Engine.Graphics.Descriptors;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.FrameGraph;

public interface IExecutionContext
{
    public VkCommandBuffer NewCmd();
    public void FreeCmd(in VkCommandBuffer cmd);

    public ICommandBufferContext UsingCmd();
    
    public DescriptorAllocator DescriptorAllocator { get; }
}