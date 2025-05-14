using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.FrameGraph;

public interface ICommandBufferContext : IDisposable, IAsyncDisposable
{
    public VkCommandBuffer Get();
}