using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan;

/// <summary>
///     Options for image barriers
/// </summary>
public struct MemoryBarrierOptions
{
    public VkAccessFlags2 SrcAccessFlags = VkAccessFlags2.VK_ACCESS_2_MEMORY_WRITE_BIT;

    public VkAccessFlags2 DstAccessFlags =
        VkAccessFlags2.VK_ACCESS_2_MEMORY_WRITE_BIT | VkAccessFlags2.VK_ACCESS_2_MEMORY_READ_BIT;

    public VkPipelineStageFlags2 WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT;
    public VkPipelineStageFlags2 NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT;

    public MemoryBarrierOptions()
    {
    }

    /// <summary>
    ///     Derives barrier stage/access masks from <see cref="GraphBufferUsage" /> - the render graph's own
    ///     buffer-transition intent, already tracked per resource action - rather than
    ///     <see cref="BufferCreateFlags" /> (creation-time usage, a different concern: what the buffer can
    ///     ever be used for, not what's happening to it at this point in the graph). Mirrors how images keep
    ///     <see cref="ImageLayout" /> separate from <see cref="ImageCreateFlags" /> for the same reason.
    /// </summary>
    public MemoryBarrierOptions(GraphBufferUsage from, GraphBufferUsage to,
        ResourceOperation fromOperation, ResourceOperation toOperation)
    {
        WaitForStages = StagesFor(from);
        NextStages = StagesFor(to);
        SrcAccessFlags = AccessFor(from, fromOperation);
        DstAccessFlags = AccessFor(to, toOperation);
    }

    private static VkPipelineStageFlags2 StagesFor(GraphBufferUsage usage)
    {
        return usage switch
        {
            GraphBufferUsage.Host or GraphBufferUsage.HostThenTransfer or GraphBufferUsage.HostThenGraphics
                or GraphBufferUsage.HostThenCompute or GraphBufferUsage.HostThenIndirect =>
                VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_HOST_BIT,
            GraphBufferUsage.Transfer => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_TRANSFER_BIT,
            GraphBufferUsage.Graphics => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_GRAPHICS_BIT,
            GraphBufferUsage.Compute => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COMPUTE_SHADER_BIT,
            GraphBufferUsage.Indirect => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_DRAW_INDIRECT_BIT,
            _ => throw new ArgumentOutOfRangeException(nameof(usage), usage, null)
        };
    }

    private static VkAccessFlags2 AccessFor(GraphBufferUsage usage, ResourceOperation operation)
    {
        return (usage, operation) switch
        {
            (GraphBufferUsage.Host or GraphBufferUsage.HostThenTransfer or GraphBufferUsage.HostThenGraphics
                or GraphBufferUsage.HostThenCompute or GraphBufferUsage.HostThenIndirect, ResourceOperation.Read) =>
                VkAccessFlags2.VK_ACCESS_2_HOST_READ_BIT,
            (GraphBufferUsage.Host or GraphBufferUsage.HostThenTransfer or GraphBufferUsage.HostThenGraphics
                or GraphBufferUsage.HostThenCompute or GraphBufferUsage.HostThenIndirect, ResourceOperation.Write) =>
                VkAccessFlags2.VK_ACCESS_2_HOST_WRITE_BIT,
            (GraphBufferUsage.Transfer, ResourceOperation.Read) => VkAccessFlags2.VK_ACCESS_2_TRANSFER_READ_BIT,
            (GraphBufferUsage.Transfer, ResourceOperation.Write) => VkAccessFlags2.VK_ACCESS_2_TRANSFER_WRITE_BIT,
            (GraphBufferUsage.Graphics or GraphBufferUsage.Compute, ResourceOperation.Read) =>
                VkAccessFlags2.VK_ACCESS_2_SHADER_STORAGE_READ_BIT,
            (GraphBufferUsage.Graphics or GraphBufferUsage.Compute, ResourceOperation.Write) =>
                VkAccessFlags2.VK_ACCESS_2_SHADER_WRITE_BIT,
            (GraphBufferUsage.Indirect, ResourceOperation.Read) => VkAccessFlags2.VK_ACCESS_2_INDIRECT_COMMAND_READ_BIT,
            (GraphBufferUsage.Indirect, ResourceOperation.Write) => VkAccessFlags2.VK_ACCESS_2_SHADER_STORAGE_WRITE_BIT,
            _ => throw new ArgumentOutOfRangeException(nameof(usage), usage, null)
        };
    }
}