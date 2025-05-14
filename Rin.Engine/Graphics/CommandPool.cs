using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

public class CommandPool
{
    private VkCommandPool _commandPool;

    public CommandPool(uint queueFamilyIndex)
    {
        _commandPool = SGraphicsModule.Get().GetDevice().CreateCommandPool(queueFamilyIndex);
    }
}