using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

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

    public MemoryBarrierOptions(BufferStage fromStage, BufferStage toStage)
    {
        WaitForStages = fromStage switch
        {
            BufferStage.Undefined => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_NONE,
            BufferStage.Transfer => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_TRANSFER_BIT,
            BufferStage.Graphics => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_GRAPHICS_BIT,
            BufferStage.Compute => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COMPUTE_SHADER_BIT,
            _ => throw new ArgumentOutOfRangeException(nameof(fromStage), fromStage, null)
        };
        NextStages = toStage switch
        {
            BufferStage.Undefined => throw new Exception("Buffer cannot transition to undefined stage"),
            BufferStage.Transfer => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_TRANSFER_BIT,
            BufferStage.Graphics => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_GRAPHICS_BIT,
            BufferStage.Compute => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COMPUTE_SHADER_BIT,
            _ => throw new ArgumentOutOfRangeException(nameof(fromStage), fromStage, null)
        };
    }

    public static MemoryBarrierOptions ComputeToTransfer()
    {
        return new MemoryBarrierOptions
        {
            WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COMPUTE_SHADER_BIT,
            NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_TRANSFER_BIT
        };
    }

    public static MemoryBarrierOptions TransferToCompute()
    {
        return new MemoryBarrierOptions
        {
            WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_TRANSFER_BIT,
            NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COMPUTE_SHADER_BIT
        };
    }

    public static MemoryBarrierOptions ComputeToGraphics()
    {
        return new MemoryBarrierOptions
        {
            WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COMPUTE_SHADER_BIT,
            NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_GRAPHICS_BIT
        };
    }

    public static MemoryBarrierOptions GraphicsToCompute()
    {
        return new MemoryBarrierOptions
        {
            WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_GRAPHICS_BIT,
            NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COMPUTE_SHADER_BIT
        };
    }

    public static MemoryBarrierOptions ComputeToAll()
    {
        return new MemoryBarrierOptions
        {
            WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COMPUTE_SHADER_BIT,
            NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT
        };
    }
}