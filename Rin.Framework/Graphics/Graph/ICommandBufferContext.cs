using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics.Graph;

public interface ICommandBufferContext : IDisposable, IAsyncDisposable
{
    public VkCommandBuffer Get();
}