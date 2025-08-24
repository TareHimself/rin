using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics.FrameGraph;

public interface ICommandBufferContext : IDisposable, IAsyncDisposable
{
    public VkCommandBuffer Get();
}