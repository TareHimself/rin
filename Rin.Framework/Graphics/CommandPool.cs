using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics;

public class CommandPool
{
    private VkCommandPool _commandPool;

    public CommandPool(uint queueFamilyIndex)
    {
        _commandPool = SGraphicsModule.Get().GetDevice().CreateCommandPool(queueFamilyIndex);
    }
}