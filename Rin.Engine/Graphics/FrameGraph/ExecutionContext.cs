using Rin.Engine.Graphics.Descriptors;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.FrameGraph;

public class ExecutionContext(CompiledGraph graph, Frame frame) : IExecutionContext
{
    private readonly VkCommandBuffer _primaryCommandBuffer = frame.GetPrimaryCommandBuffer();
    //private bool _primaryAvailable = true;


    public VkCommandBuffer GetCommandBuffer()
    {
        return _primaryCommandBuffer;
    }

    public DescriptorAllocator DescriptorAllocator { get; } = frame.GetDescriptorAllocator();
}