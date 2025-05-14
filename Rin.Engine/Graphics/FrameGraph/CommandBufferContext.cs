using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.FrameGraph;

public sealed class CommandBufferContext(ExecutionContext executionContext,VkCommandBuffer cmd) : ICommandBufferContext
{
    public void Dispose()
    {
        executionContext.FreeCmd(cmd);
    }

    public ValueTask DisposeAsync()
    {
        executionContext.FreeCmd(cmd);
        return ValueTask.CompletedTask;
    }

    public VkCommandBuffer Get()
    {
        return cmd;
    }
}